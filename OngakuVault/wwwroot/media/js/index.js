// Script for the index.html page
// Requires: bulma.js, utils.js
// INIT API & WS ENDPOINTS
var APIEndpointProtocol = 'http://'; 
var WebSocketEndpointProtocol = 'ws://';
if (window.location.protocol === 'https:') {
    APIEndpointProtocol = 'https://';
    WebSocketEndpointProtocol = 'wss://';
}
const APIEndpoint = `${APIEndpointProtocol}${window.location.host}/api`;
const WebSocketEndpoint = `${WebSocketEndpointProtocol}${window.location.host}/ws`;

var noResultsTemplateHTMLElement; // Contains the noResults HTML Element
var jobItemTemplateHTMLElement; // Contains a job item template HTML Element;

// Run when DOM finished loading
document.addEventListener('DOMContentLoaded', () => {
    // [Main DOM]
    const searchInput = document.getElementById("searchInput")
    const searchButton = document.getElementById("searchButton")
    const resultsDiv = document.getElementById("results")
    // [Modals]
    const cancelJobModal = document.getElementById("cancel-job-modal")
    const downloadMediaJobCreationModal = document.getElementById("download-media-job-creation-modal")
    // [Inside JobCreationModal]
    const finalAudioFormat = document.getElementById("finalAudioFormat")
    // Make a copy of the no results HTML Element
    noResultsTemplateHTMLElement = document.getElementById("no-results-template").cloneNode(true)
    noResultsTemplateHTMLElement.classList.remove("is-hidden")
    // Make a copy of a job item (to use as a template)
    jobItemTemplateHTMLElement = document.getElementById("job-item-template").cloneNode(true)
    jobItemTemplateHTMLElement.classList.remove("is-hidden")

    // If the enter key is pressed inside searchInput, do a search
    searchInput.addEventListener('keyup', (event) => {
        if (event.key === 'Enter') {
            // Ensure a research is not already on-going
            if (!searchInput.classList.contains("is-loading") && !searchButton.classList.contains("is-loading")) {
                doSearch();
            }
        }
    });
    searchButton.addEventListener('click', doSearch);

    // Listen for all interactions inside the results div
    resultsDiv.addEventListener('click', (event) => {
        // Check if the clicked element is a job cancel button
        if (event.target && event.target.id == 'cancel-job') {
            const jobItemElement = event.target.parentElement; // Get the job item element
            const jobId = jobItemElement.id.replace("job-item_", ""); // Get the job id out of the jobItem element id
            openModalQuestion(cancelJobModal).then((didConfirm) => {
                if (didConfirm) {
                    fetch(`${APIEndpoint}/job/${jobId}/cancel`, { method: 'DELETE' }).then(async (response) => {
                        if (!response.ok) {
                            showWarning(`Failed to cancel selected job. Server responded with status ${response.status}.\nError: ${await response.text()}`);
                            console.error(`Failed to cancel job id '${jobId}', api response code : ${response.status}`)
                        }
                        closeModal(cancelJobModal); // Free the modal
                    });
                }
            })
        }
    });

    // Listen for interactions on the select input element (finalAudioFormat)
    const modalWarningLosslessRecommended = document.getElementById("modal-warning-LosslessRecommended")
    const modalWarningLosslessNotRecommended = document.getElementById("modal-warning-LosslessNotRecommended")
    finalAudioFormat.addEventListener('change', (event) => {
        // Get the selected option element
        const selectedOption = event.target.options[event.target.selectedIndex];
        // Function to hide all warnings
        const hideWarnings = () => {
            if (!modalWarningLosslessNotRecommended.classList.contains("is-hidden")) {
                animateCSS(modalWarningLosslessNotRecommended, "flipOutX").then(() => {
                    modalWarningLosslessNotRecommended.classList.add("is-hidden");
                })
            } else if (!modalWarningLosslessRecommended.classList.contains("is-hidden")) {
                animateCSS(modalWarningLosslessRecommended, "flipOutX").then(() => {
                    modalWarningLosslessRecommended.classList.add("is-hidden");
                })
            }
        }
        // Get the content of the data-mediaInfoJson attribute
        const currentMediaInfoString = downloadMediaJobCreationModal.getAttribute("data-mediaInfoJson");
        if (currentMediaInfoString) {
            const currentMediaInfoJson = JSON.parse(currentMediaInfoString);
            const isLosslessRecommended = currentMediaInfoJson.isLosslessRecommended;
            // If a lossless audio is available, but lossy format is selectionned
            if (isLosslessRecommended && selectedOption.matches("[is-lossy]")) {
                modalWarningLosslessRecommended.classList.remove("is-hidden");
                animateCSS(modalWarningLosslessRecommended, "flipInX")
            } else if (!isLosslessRecommended && selectedOption.matches("[is-lossless]")) { // If lossy is available / best quality available, but lossless format is selectionned
                modalWarningLosslessNotRecommended.classList.remove("is-hidden");
                animateCSS(modalWarningLosslessNotRecommended, "flipInX")
            } else {
                // Hide the visible warnings
                hideWarnings();
            }
        } else {
            console.log("User seems to be in manual mode, cannot verify for lossless recommendation.");
            hideWarnings();
        }
    });

    // Fetch current jobs list from the server & connect to websocket
    fetch(`${APIEndpoint}/job/all`).then(async (response) => {
        if (!response.ok) {
            showWarning(`Failed to fetch the jobs list. Server responded with status ${response.status}.\nError: ${await response.text()}`);
            console.error(`Failed to get the jobs list, api response code : ${response.status}`)
        } else {
            const responseJsonCurrentJobs = await response.json();

            if (responseJsonCurrentJobs.length >= 1) {
                clearResults(); // Ensure there are not childs in the results element
                responseJsonCurrentJobs.forEach((currentJobModel) => {
                    // Add the job to the results list
                    createJobItem(currentJobModel);
                })
            } else showNoResultMessage();
            // Disable the skeleton loading animation on the results element
            resultsDiv.classList.remove("is-skeleton", "is-skeleton-larger");

            // Create a connection with the server websocket to listen to live status update
            const socket = new WebSocket(WebSocketEndpoint);

            // Listen for the connected event
            socket.addEventListener("open", () => {
                console.log("Successfully opened a websocket connection with server.");
            });

            // Listen for new messages from the server
            socket.addEventListener("message", (event) => {
                try {
                    const webSocketBroadcastDataJson = JSON.parse(event.data);
                    const messageKey = webSocketBroadcastDataJson.key; // Tell us how to handle the message
                    const message = webSocketBroadcastDataJson.data; // The message
                    const jobItemElement = getJobItemByID(message.id); // The jobItemElement affected by the message, if it exist (else it's null) 
                    console.debug(`WebSocket: Reiceived a message from server with key: '${messageKey}'.`);
                    switch (messageKey)
                    {
                        // New job reported as queued, add it to the results list if not already present
                        case "NewJobWasQueued": // Here, message is a JobModel (see swagger schema)
                            if (jobItemElement === null)
                            {
                                createJobItem(message);
                            }
                            break;
                        // Existing job reported status update, update it's status
                        case "JobReportedStatusUpdate": // Here message is a JobStatusReportModel
                            if (jobItemElement !== null) {
                                updateJobItemProgress(jobItemElement, message.status, message.progress, message.progressTaskName);
                            } else {
                                console.warn(`Websocket: Reiceived a status update for job id '${message.id}', but couldn't locate a job item in the DOM with this ID.'`);
                            } 
                            break;
                        default:
                            console.warn(`WebSocket: Reiceived a message from server with key: '${messageKey}'. But the client does not know how to handle it.`);
                            break;
                    }
                } catch (ex) {
                    console.error("WebSocket: Received a message, but failed while handling the message.", ex, event);
                }
            });

            socket.addEventListener("close", () => {
                console.log("WebSocket: Connection with server was closed.");
            });

            socket.addEventListener("error", (error) => {
                console.error("WebSocket error:", error);
            });
            
        }
    });

    
});

/**
 * Overwrite the warning modal message and show the modal.
 * @param {String} message The message to show
 */
function showWarning(message)
{
    const warningModal = document.getElementById("warning-modal")
    const warningModalMessage = document.getElementById("warning-message")
    warningModalMessage.innerText = message;
    openModal(warningModal);
}

/**
 * Called when the user want to search media information on a website (mediaUrl) and create
 * a job on the server side.
 */
async function doSearch() {
    toggleLoadingAnimation();
    // Get user inputs
    const selectedInterrogationMode = document.getElementById("searchInterrogationMode").value
    const mediaUrl = document.getElementById("searchInput").value
    const downloadMediaJobCreationModal = document.getElementById("download-media-job-creation-modal")
    var isMediaFetchValid = false; // Default to false, don't show the confirmation modal

    if (isUrlValid(mediaUrl)) {
        console.log(`Input URL : ${mediaUrl} | Interrogation Mode : ${selectedInterrogationMode}`);
        if (selectedInterrogationMode == "automatic") {
            // Fetch basic information to the server by using the API
            try {
                const response = await fetch(`${APIEndpoint}/media/info?mediaUrl=${mediaUrl}`);
                if (!response.ok) {
                    showWarning(`Failed to fetch media info. Server responded with status ${response.status}.\nError: ${await response.text()}`);
                    // Stop execution
                    throw new Error(`Failed to get media info, api response code : ${response.status}`);
                }

                const jsonResponse = await response.json();
                /* Here we put the server json response into a attribute on the modal. The modal will perform
                   verifications based on the user inputs. For example, verification of the final file type for the
                   lossless & lossy codec warning will be done using this attribute.
                */
                downloadMediaJobCreationModal.setAttribute("data-mediaInfoJson", JSON.stringify(jsonResponse))

                // If lossless codec is available, select flac file format by default
                if (jsonResponse.isLosslessRecommended) {
                    const selectFileFormatElement = downloadMediaJobCreationModal.querySelector("#finalAudioFormat")
                    selectFileFormatElement.value = "flac";
                    // Trigger the update event since we updated from the js side
                    selectFileFormatElement.dispatchEvent(new Event("change"))
                }

                // We fill DOM elements of the modal that have a matching ID name with a key name from the json.
                // Loop through the JSON keys
                Object.keys(jsonResponse).forEach(key => {
                    // Find the element with the matching ID
                    const element = downloadMediaJobCreationModal.querySelector(`#${key}`);
                    if (element) {
                        // Ensure of the element type
                        if (element.tagName === "TEXTAREA" || element.tagName === "INPUT") {
                            element.value = jsonResponse[key]; // For input and textareas elements
                        }
                    }
                });
                isMediaFetchValid = true;
            } catch (error) {
                console.error(error.message);
            }

        } else {
            // Manual, we don't have any information from the server
            downloadMediaJobCreationModal.removeAttribute("data-mediaInfoJson"); // Ensure no previously fetched data is present
            isMediaFetchValid = true;
        }

        if (isMediaFetchValid) {
            openModalQuestion(downloadMediaJobCreationModal).then(async (isConfirm) => {
                if (isConfirm) {
                    const newMediaInfoModalForm = downloadMediaJobCreationModal.querySelector("#MediaInfo-form");
                    const newConfigurationModalForm = downloadMediaJobCreationModal.querySelector("#JobConfiguration-form");
                    var newMediaInfoModelJson = getFormDataAsJSON(newMediaInfoModalForm);
                    var newJobConfigurationModelJson = getFormDataAsJSON(newConfigurationModalForm);

                    // Add the mediaUrl to the MediaInfoModel json
                    newMediaInfoModelJson["mediaUrl"] = mediaUrl;
                    // Create the json object (JobRESTCreationModel) that will be send to the api
                    const newJobRESTCreationModelJson = {
                        mediaInfo: newMediaInfoModelJson,
                        jobConfiguration: newJobConfigurationModelJson,
                    }
                    const response = await fetch(`${APIEndpoint}/job/create`, {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify(newJobRESTCreationModelJson)
                    });

                    if (!response.ok) {
                        showWarning(`Failed to create a new job. Server responded with status ${response.status}.\nError: ${await response.text()}`);
                        // Stop execution
                        throw new Error(`Failed to create a new job, api response code : ${response.status}`);
                    }

                    const jobModelResponseJson = await response.json();
                    console.log(`Server created job ID '${jobModelResponseJson.id}' for '${mediaUrl}', waiting for websocket to report the job as queued...`);
                    closeModal(downloadMediaJobCreationModal);
                }
                // Clear all inputs inside the MediaJobCreationModal
                clearForm(downloadMediaJobCreationModal);
            })
        }
    } else {
        showWarning(`The url '${mediaUrl}' is not a valid url. Your url need to start with 'http:\\\\' or 'https:\\\\' followed by a domain and top domain.`);
    }
    toggleLoadingAnimation();
}

/**
 * Enable & Disable the loading css animation on the button & input.
 */
function toggleLoadingAnimation() {
    document.getElementById("searchInput").parentElement.classList.toggle("is-loading")
    document.getElementById("searchButton").classList.toggle("is-loading")
}

/**
 * Show the 'There are no results' message in the results container.
 * *Will clear everything in the results container.
 */
function showNoResultMessage() {
    clearResults()
    const resultsElement = document.getElementById("results");
    const newItem = noResultsTemplateHTMLElement.cloneNode(true);
    resultsElement.appendChild(newItem);
}

/**
 * Return the job item element or null if not found
 * @param {String} jobID The ID of the job on the server side
 * @returns {HTMLDivElement} The div element or null if not found
 */
function getJobItemByID(jobID) {
    return document.getElementById(`job-item_${jobID}`)
}

/**
 * Create a item entry inside the results element.
 * Will also clear all child-element of the results element if there isn't a single element
 * starting with ID "job-item_" (so a job item element). Allowing the removal of NoResults elements
 * for example.
 * @param {String} jobModel The data of a job (represended as JobModel on swagger)
 * @returns {HTMLDivElement} The created Job Item element
 */
function createJobItem(jobModel) {
    const resultsElement = document.getElementById("results")
    const newItem = jobItemTemplateHTMLElement.cloneNode(true);
    newItem.id = `job-item_${jobModel.id}`;
    // Get the element with ID Title inside the newItem
    newItem.querySelector("#Title").textContent = jobModel.data.name;
    // Set the progress of the current job
    updateJobItemProgress(newItem, jobModel.status, jobModel.progress, jobModel.progressTaskName)
    // Get all child element with a ID attribute, and verify if they start with "job-item_". Then invert the bool result
    const shouldResultsBeCleared = !(Array.from(resultsElement.querySelectorAll("[id]")).some(element => element.id.startsWith("job-item_")));
    if (shouldResultsBeCleared) clearResults(); // If shouldResultsBeCleared is true, clear results element childs
    // Add our JobItem element as results element child
    resultsElement.appendChild(newItem);
    return newItem
}

/**
 * Update on the DOM the specified job item progress related elements
 * @param {HTMLDivElement} jobItemHTMLElement The Job Item element to edit
 * @param {String} status The new status
 * @param {Number} progress The new progress (0-100 int)
 * @param {String} progressTaskName The new progress task name
 */
function updateJobItemProgress(jobItemHTMLElement, status, progress, progressTaskName)
{
    const progressElement = jobItemHTMLElement.querySelector("#Progress");
    const progressBarElement = jobItemHTMLElement.querySelector("#ProgressBar");
    const progressTaskNameElement = jobItemHTMLElement.querySelector("#ProgressTaskName");
    
    switch (status)
    {
        case "Queued":
            progressBarElement.className = "progress is-info";
            progressElement.innerText = "Queued";
            break;
        case "Running":
            progressBarElement.className = "progress is-link";
            progressBarElement.value = progress;
            progressElement.innerText = `${progress}%`;
            progressTaskNameElement.innerText = progressTaskName;
            break;
        case "Completed":
            progressBarElement.className = "progress is-success";
            progressBarElement.value = progress;
            progressElement.innerText = `${progress}%`;
            progressTaskNameElement.innerText = progressTaskName;
            break;
        case "Failed":
        case "Cancelled":
            progressBarElement.className = "progress is-danger";
            progressBarElement.value = progress;
            progressElement.innerText = `${progress}%`;
            progressTaskNameElement.innerText = progressTaskName;
            break;
        default:
            console.warn(`Unsupported job status '${status}' was given for a job item progress update.`);
            break;
    }
}

/**
 * Remove every children of the results container
 */
function clearResults() {
    const resultsElement = document.getElementById("results")
    resultsElement.replaceChildren();
}

/**
 * Will clear all input childs-element of a form
 * @param {Element} parentElement The form itself
 */
function clearForm(parentElement) {
    // Get all input, select, and textarea elements within the parent element
    const inputs = parentElement.querySelectorAll('input, select, textarea');

    inputs.forEach(input => {
        // Handle different type of inputs
        if (input.type === 'checkbox' || input.type === 'radio') {
            input.checked = false;
        } else if (input.tagName === 'SELECT') {
            // For select elements, the priority is the 'selected' attribute
            const options = input.querySelectorAll('option');
            var foundSelected = false;

            options.forEach(option => {
                if (option.hasAttribute('selected')) {
                    // If the option has the 'selected' attribute we select it
                    option.selected = true;
                    foundSelected = true;
                }
            });

            // If no option had the 'selected' attribute we select the first option
            if (!foundSelected && options[0]) {
                options[0].selected = true;
            }
        } else {
            // For text input and textarea we clear the value
            input.value = '';
        }

        // Trigger a change event for the element we just updated
        input.dispatchEvent(new Event('change'));
    });
}