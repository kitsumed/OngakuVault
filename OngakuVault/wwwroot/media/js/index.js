// Script for the index.html page
// Requires: bulma.js, utils.js
// INIT API & WS ENDPOINTS
let APIEndpointProtocol = 'http://';
let WebSocketEndpointProtocol = 'ws://';
if (window.location.protocol === 'https:') {
    APIEndpointProtocol = 'https://';
    WebSocketEndpointProtocol = 'wss://';
}
const APIEndpoint = `${APIEndpointProtocol}${window.location.host}/api`;
const WebSocketEndpoint = `${WebSocketEndpointProtocol}${window.location.host}/ws`;

let noResultsTemplateHTMLElement; // Contains the noResults HTML Element
let jobItemTemplateHTMLElement; // Contains a job item template HTML Element;
let lyricElementTemplateHTMLElement; // Contains a lyric element template
let metadataValueSeparator = ';'; // Default separator for multiple values (artist, genre)

// Run when DOM finished loading
document.addEventListener('DOMContentLoaded', () => {
    // [Main DOM]
    const searchInput = document.getElementById("searchInput")
    const searchButton = document.getElementById("searchButton")
    const resultsDiv = document.getElementById("results")
    // [Modals]
    const cancelJobModal = document.getElementById("cancel-job-modal")
    const jobCreationModal = document.getElementById("download-media-job-creation-modal")
    const lyricsOffsetModal = document.getElementById("lyrics-offset-modal")
    // [Inside JobCreationModal]
    const jobCreationModalFinalAudioFormat = document.getElementById("finalAudioFormat")
    const jobCreationModalLyrics = document.getElementById("JobConfiguration-lyrics")
    // [Inside LyricsOffsetModal]
    const lyricsOffsetModalForum = document.getElementById("lyrics-offset-forum")
    // Make a copy of a lyric element HTML Element
    lyricElementTemplateHTMLElement = jobCreationModalLyrics.querySelector("#lyric").cloneNode(true);
    // Make a copy of the no results HTML Element
    noResultsTemplateHTMLElement = document.getElementById("no-results-template").cloneNode(true)
    noResultsTemplateHTMLElement.classList.remove("is-hidden")
    // Make a copy of a job item (to use as a template)
    jobItemTemplateHTMLElement = document.getElementById("job-item-template").cloneNode(true)
    jobItemTemplateHTMLElement.classList.remove("is-hidden")

    // Fetch the metadata separator from the server and initialize multi-value field support
    initializeMultiValueFields();

    // == REGISTER EVENTS ==

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
                    fetch(`${APIEndpoint}/job/cancel/${jobId}`, { method: 'DELETE' }).then(async (response) => {
                        if (!response.ok) {
                            showWarning(`Failed to cancel selected job. Server responded with status ${response.status}.\nError: ${await response.text()}`);
                            console.error(`Failed to cancel job id '${jobId}', api response code : ${response.status}`, response)
                        }
                        closeModal(cancelJobModal); // Free the modal
                    });
                }
            })
        }
    });

    // Listen for interactions on the select input element (jobCreationModalFinalAudioFormat)
    const modalWarningLosslessRecommended = document.getElementById("modal-warning-LosslessRecommended")
    const modalWarningLosslessNotRecommended = document.getElementById("modal-warning-LosslessNotRecommended")
    jobCreationModalFinalAudioFormat.addEventListener('change', (event) => {
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
        const currentMediaInfoString = jobCreationModal.getAttribute("data-mediaInfoJson");
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

    // Handle lyrics related buttons
    jobCreationModalLyrics.addEventListener('click', (event) => {
        // Verify if the button is a search lyrics button
        if (event.target && event.target.id == 'search-lyrics-button')
        {
            let buttonElement = event.target;
            // If the clicked element is a IMG, we want to get the parent button
            if (event.target.tagName === "IMG")
            {
                buttonElement = event.target.parentElement.parentElement
            }
            let finalUrl = buttonElement.getAttribute("prefixUrl")
            // Get the track name and put it between the prefix and suffix url
            finalUrl += jobCreationModal.querySelector('#MediaInfo-form input[id="name"]').value ?? "nameHere"
            finalUrl += buttonElement.getAttribute("suffixUrl") ?? ""
            window.open(finalUrl, '_blank', 'noopener, noreferrer');
        } // [BUTTONS RELATED TO LYRICS]
        else if (event.target && event.target.id == 'remove-lyric') { // Check if the clicked element is a remove button
            const currentLyricElement = event.target.parentElement;
            // Ensure at least one lyric element exist before removing
            const allLyricElements = jobCreationModalLyrics.querySelectorAll("#lyric");
            if (allLyricElements.length >= 2) {
                currentLyricElement.remove();
            } else {
                console.log("User cannot remove the last lyric element.");
                // Remove all lyrics element and add one back, this clear the content of the last lyric element.
                removeLyricElements();
                addLyricElement(); // Add one empty lyric element
            }
        } else if (event.target && event.target.id == 'add-lyric') { // Check for add button
            addLyricElement();
        } else if (event.target && event.target.id == 'time-offset') { // Check for offset button
            openModalQuestion(lyricsOffsetModal).then((didApply) => {
                if (didApply)
                {
                    const offsetModalForum = getFormDataAsJSON(lyricsOffsetModalForum);
                    const offsetTime = convertStringFormatToMilliseconds(offsetModalForum.offsetTime)
                    // Get all of the lyrics elements
                    const lyricsElements = jobCreationModalLyrics.querySelectorAll("#lyric");
                    if (lyricsElements.length >= 1 && offsetTime !== null) {
                        // Loop trought all user created lyric
                        for (let i = 0; i < lyricsElements.length; i++) {
                            const currentLyricTimeElement = lyricsElements[i].querySelector("#lyric-time");
                            // Ensure lyrics is not empty
                            if (currentLyricTimeElement.value.length !== 0) {
                                const originalTime = convertStringFormatToMilliseconds(currentLyricTimeElement.value)
                                if (offsetModalForum.offsetType === "positive") {
                                    currentLyricTimeElement.value = convertMillisecondsToStringFormat(originalTime + offsetTime)
                                } else if (offsetModalForum.offsetType === "negative") {
                                    const newTimeMS = originalTime - offsetTime;
                                    // If the negative offset made a time go in negative, we remove the lyrics element connected to it
                                    if (newTimeMS < 0) {
                                        // Ensure we don't remove the lyric element if it's the only one left
                                        if (lyricsElements.length !== 1) {
                                            lyricsElements[i].remove();
                                        } else {
                                            currentLyricTimeElement.value = ""
                                        }
                                    } else {
                                        currentLyricTimeElement.value = convertMillisecondsToStringFormat(newTimeMS)
                                    }
                                } else {
                                    console.warn("Invalid offsetType selected.", offsetModalForum)
                                }
                            } 
                        }
                    }
                    closeModal(lyricsOffsetModal);
                }
            });
        } else if (event.target && event.target.id == 'clear-lyrics-time') { // Clear all times inputs
            if (confirm("Do you want to clear all lyrics elements time values?") == true) {
                const allLyricTimeElements = jobCreationModalLyrics.querySelectorAll("#lyric #lyric-time");
                // Loop trought all lyric-time elements
                for (var i = 0; i < allLyricTimeElements.length; i++) {
                    // Reset time element
                    allLyricTimeElements[i].value = "";
                }
            }
        } else if (event.target && event.target.id == 'remove-all-lyrics') { // Remove all lyrics
            if (confirm("Do you want to clear all existing lyrics elements?") == true) {
                removeLyricElements();
                addLyricElement(); // Add one empty lyric element
            }
        } else if (event.target && event.target.id == 'remove-empty-lyrics') // Remove empty lyrics
        {
            if (confirm("Do you want to clear all lyrics elements with empty content?") == true) {
                removeEmptyLyricElements();
                // Ensure that at least one lyric element still exist in the list
                const allLyricElements = jobCreationModalLyrics.querySelectorAll("#lyric");
                if (allLyricElements.length === 0) {
                    addLyricElement();
                }
            }
        }
    });

    // Handle lyrics time format & the button to load lyrics from file
    jobCreationModalLyrics.addEventListener('change', (event) => {
        // Check if the element that triggered the input event is a lyric time element
        if (event.target && event.target.id == 'lyric-time') {
            const currentLyricTime = event.target;
            const formattedTime = verifyAndFormatTime(currentLyricTime.value);
            if (formattedTime) {
                currentLyricTime.value = formattedTime;
            } else if (currentLyricTime.value !== "") {
                currentLyricTime.value = "00:00.00";
            }
        } else if (event.target && event.target.id == 'load-lyrics-from-file') {
            const animatedButton = event.target.parentElement.querySelector("span");
            const currentLyricSelectedFile = event.target.files[0]
            if (!currentLyricSelectedFile) return;

            if (currentLyricSelectedFile.size > 4194304) // 4MB in bytes
            {
                showWarning("Your lyrics file cannot be bigger than 4MB.");
                return;
            }

            // Simulate as if the input element was in a form that got submitted
            const formData = new FormData();
            formData.append("File", currentLyricSelectedFile)

            animatedButton.classList.toggle("is-loading");
            // Send request to server
            fetch(`${APIEndpoint}/Lyric/getLyricsFromFile`, { method: "POST", body: formData }).then(async (response) => {
                animatedButton.classList.toggle("is-loading");
                if (!response.ok) {
                    showWarning(`Could not parse lyrics. Server responded with status ${response.status}.\nError: ${await response.text()}`);
                    console.error(`Failed to parse lyrics, api response code : ${response.status}`, response);
                    return;
                }
                const responseLyrics = await response.json();
                console.log(`Found ${responseLyrics.length} lyrics inside the uploaded file.`)
                if (responseLyrics && responseLyrics.length >= 1) {
                    addLyricElement(responseLyrics)
                } else showWarning("The server parsed the uploaded file but did not find any lyrics.");
            });
        }
    });

    // Handle changes on the lyrics offset modal
    lyricsOffsetModal.addEventListener('change', (event) => {
        // Check if the element that triggered the input event is the lyric offset time
        if (event.target && event.target.id == 'offsetTime') {
            const currentLyricTime = event.target;
            const formattedTime = verifyAndFormatTime(currentLyricTime.value);
            if (formattedTime) {
                currentLyricTime.value = formattedTime;
            } else if (currentLyricTime.value !== "") {
                currentLyricTime.value = "00:00.00";
            }
        }
    });

    // Fetch current jobs list from the server & connect to websocket
    fetch(`${APIEndpoint}/job/all`).then(async (response) => {
        if (!response.ok) {
            showWarning(`Failed to fetch the jobs list. Server responded with status ${response.status}.\nError: ${await response.text()}`);
            console.error(`Failed to get the jobs list, api response code : ${response.status}`, response)
        } else {
            const responseJsonCurrentJobs = await response.json();

            if (responseJsonCurrentJobs.length >= 1) {
                clearResults(); // Ensure there are not childs in the results element
                responseJsonCurrentJobs.forEach((currentJobModel) => {
                    // Add the job to the results list
                    createJobItem(currentJobModel);
                })
            } else showNoJobsMessage();
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
                    switch (messageKey) {
                        // New job reported as queued, add it to the results list if not already present
                        case "NewJobWasQueued": // Here, message is a JobModel (see swagger schema)
                            if (jobItemElement === null) {
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
                        // Server reported the removal of a job from it's memory
                        case "JobDestroyed": // Here message is a string (the job ID)
                            let removedJobItem = getJobItemByID(message);
                            removedJobItem.remove(); // Remove the job element from DOM
                            IfEmptyShowNoJobsMessage();
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
                showWarning("WebSocket connection with server was closed, jobs status won't be updated anymore.\nReload your page to reconnect.");
            });

            socket.addEventListener("error", (error) => {
                console.error("WebSocket error:", error);
            });

        }
    });

    // == FUNCTIONS ==

    /**
    * Overwrite the warning modal message and show the modal.
    * @param {String} message The message to show
    */
    function showWarning(message) {
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
        let isMediaFetchValid = false; // Default to false, don't show the confirmation modal

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
                    // Populate the lyrics element list if relevant
                    if (jsonResponse.lyrics.length >= 1) {
                        // Add lyric element for each MediaLyric model
                        addLyricElement(jsonResponse.lyrics);
                        // Remove the lyric elements with empty content (like the default first lyric element)
                        removeEmptyLyricElements();
                    }
                    isMediaFetchValid = true;
                } catch (error) {
                    console.error("Failed to handle advanced media information returned by server.", error.message);
                }

            } else {
                // Manual, we don't have any information from the server
                downloadMediaJobCreationModal.removeAttribute("data-mediaInfoJson"); // Ensure no previously fetched data is present
                // Ensure only one lyric element exists
                removeLyricElements();
                addLyricElement();
                isMediaFetchValid = true;
            }

            if (isMediaFetchValid) {
                openModalQuestion(downloadMediaJobCreationModal).then(async (isConfirm) => {
                    if (isConfirm) {
                        const newMediaInfoModalForm = downloadMediaJobCreationModal.querySelector("#MediaInfo-form");
                        const newConfigurationModalForm = downloadMediaJobCreationModal.querySelector("#JobConfiguration-form");
                        const jobCreationModalLyrics = downloadMediaJobCreationModal.querySelector("#JobConfiguration-lyrics")
                        // Get all of the downloadMediaJobCreationModal forms data
                        let newMediaInfoModelJson = getFormDataAsJSON(newMediaInfoModalForm);
                        let newJobConfigurationModelJson = getFormDataAsJSON(newConfigurationModalForm);
                        // [LYRIC-DATA PROCESSING]
                        let newJobConfigurationModelLyricsJson = [];
                        // Get all of the lyrics elements
                        const lyricsElements = jobCreationModalLyrics.querySelectorAll("#lyric");
                        let shouldLyricsBeProcessed = false
                        // If the number of lyrics is 1 (default) or bigger, the user may have not defined any lyrics,
                        // so we verify if the first lyric-content element is empty or not before applying custom lyrics.
                        if (lyricsElements && lyricsElements.length >= 1) {
                            shouldLyricsBeProcessed = lyricsElements[0].querySelector("#lyric-content").value.length !== 0 // False if content is empty
                        }
                        if (shouldLyricsBeProcessed) {
                            // Loop trought all user created lyric
                            for (let i = 0; i < lyricsElements.length; i++) {
                                const currentLyricTime = lyricsElements[i].querySelector("#lyric-time").value;
                                let lyricModel;
                                // Lyrics has a time
                                if (currentLyricTime.length !== 0) {
                                    lyricModel = {
                                        content: lyricsElements[i].querySelector("#lyric-content").value ?? "",
                                        time: convertStringFormatToMilliseconds(currentLyricTime),
                                    }
                                } else {
                                    lyricModel = {
                                        content: lyricsElements[i].querySelector("#lyric-content").value ?? ""
                                    }
                                }
                                // Add the lyric to the json lyrics list
                                newJobConfigurationModelLyricsJson.push(lyricModel);
                            }
                            // Create the lyrics key on the job configuration with our json lyrics list
                            newJobConfigurationModelJson["lyrics"] = newJobConfigurationModelLyricsJson;
                        }

                        // [CREATING JSON POST REQUEST]
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
     * Show the 'There are no jobs' message in the results container.
     * *Will clear everything in the results container.
     */
    function showNoJobsMessage() {
        clearResults()
        const resultsElement = document.getElementById("results");
        const newItem = noResultsTemplateHTMLElement.cloneNode(true);
        resultsElement.appendChild(newItem);
    }

    /**
     * Show the 'There are no jobs' message in the results container if there isn't any jobs.
     */
    function IfEmptyShowNoJobsMessage() {
        const resultsElement = document.getElementById("results");
        if (resultsElement.childElementCount === 0) {
            showNoJobsMessage();
        } 
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
    function updateJobItemProgress(jobItemHTMLElement, status, progress, progressTaskName) {
        const progressElement = jobItemHTMLElement.querySelector("#Progress");
        const progressBarElement = jobItemHTMLElement.querySelector("#ProgressBar");
        const progressTaskNameElement = jobItemHTMLElement.querySelector("#ProgressTaskName");

        switch (status) {
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
                let foundSelected = false;

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

    /**
     * Remove all lyrics element inside the JobConfiguration-lyrics.
     */
    function removeLyricElements() {
        // JobConfiguration-lyrics is part of the JobCreationModal
        const allLyricElements = jobCreationModalLyrics.querySelectorAll("#lyric");
        // Loop trought all lyric elements
        for (let i = 0; i < allLyricElements.length; i++) {
            allLyricElements[i].remove();
        }
    }

    /**
     * Remove all lyrics element that have their content input empty.
     */
    function removeEmptyLyricElements() {
        // JobConfiguration-lyrics is part of the JobCreationModal
        const allLyricElements = jobCreationModalLyrics.querySelectorAll("#lyric");
        // Loop trought all lyric elements
        for (let i = 0; i < allLyricElements.length; i++) {
            // Ensure the value is empty, null, undefined, etc
            if (!allLyricElements[i].querySelector("#lyric-content").value) {
                allLyricElements[i].remove();
            }
        }
    }

    /**
     * Create one or multiple lyric element. If a list of MediaLyric is given, a lyric element will be created and
     * populated for each MediaLyric. If the value is not defined, one empty lyric element will be created.
     * @param {Array} lyricsArray A list of MediaLyric (see swagger docs)
     */
    function addLyricElement(lyricsArray) {
        const lyricsButtonsContainer = jobCreationModalLyrics.querySelector("#lyrics-buttons-container");
        let numberOfElements = 1
        if (lyricsArray != null && lyricsArray.length >= 2) {
            numberOfElements = lyricsArray.length;
        }

        // Create one/multiple lyric element
        for (let i = 0; i < numberOfElements; i++) {
            // Clone the lyric element template and add it to the DOM
            const newLyricElement = lyricElementTemplateHTMLElement.cloneNode(true);
            let lyricTimeValue = "";
            let lyricContentValue = "";
            if (lyricsArray != null) {
                if (lyricsArray[i].time != null) {
                    lyricTimeValue = convertMillisecondsToStringFormat(lyricsArray[i].time)
                }
                if (lyricsArray[i].content != null) {
                    lyricContentValue = lyricsArray[i].content
                }
            }
            // Reset the fields of our lyric element copy
            newLyricElement.querySelector("#lyric-time").value = lyricTimeValue;
            newLyricElement.querySelector("#lyric-content").value = lyricContentValue;
            // Put the new lyric before the all of the lyrics related buttons
            jobCreationModalLyrics.insertBefore(newLyricElement, lyricsButtonsContainer);
        }
    }

    /**
     * Initialize multi-value field support by fetching the separator from the server
     * and setting up event listeners for visual preview.
     */
    async function initializeMultiValueFields() {
        try {
            // Fetch the metadata separator from server
            const response = await fetch(`${APIEndpoint}/Settings/metadata-separator`);
            if (response.ok) {
                const separator = await response.json();
                // Validate the response is a single character string
                if (typeof separator === 'string' && separator.length === 1) {
                    metadataValueSeparator = separator;
                    console.log(`Metadata value separator loaded: '${metadataValueSeparator}'`);
                } else {
                    console.warn("Invalid separator received from server, using default:", metadataValueSeparator);
                }
            }
        } catch (error) {
            console.warn("Failed to fetch metadata separator from server, using default:", metadataValueSeparator);
        }

        // Update the separator character display in help text
        const separatorChars = document.querySelectorAll('.separator-char');
        separatorChars.forEach(el => el.textContent = metadataValueSeparator);

        // Get all multi-value input fields by data attribute
        const multiValueInputs = document.querySelectorAll('[data-multi-value-field="true"]');
        
        multiValueInputs.forEach(input => {
            // Get the help text element by ID pattern: {inputId}-separator-help
            const helpText = document.getElementById(`${input.id}-separator-help`);
            
            // Add focus event listener to show help text with animation
            input.addEventListener('focus', () => {
                if (helpText && helpText.classList.contains('is-hidden')) {
                    helpText.classList.remove('is-hidden');
                    animateCSS(helpText, 'fadeInLeft');
                }
            });

            // Add blur event listener to hide default help text
            input.addEventListener('blur', () => {
                if (helpText && !helpText.dataset.hasMultipleValues) {
                    helpText.classList.add('is-hidden');
                }
            });

            // Add change event listener for updating help text content
            input.addEventListener('change', () => updateMultiValueHelp(input, helpText));
        });
    }

    /**
     * Update the help text for a multi-value input field.
     * @param {HTMLInputElement} input The input element
     * @param {HTMLElement} helpText The help text paragraph element
     */
    function updateMultiValueHelp(input, helpText) {
        const value = input.value.trim();
        const fieldType = input.dataset.fieldType || 'value'; // 'artist' or 'genre'
        
        if (!value || !value.includes(metadataValueSeparator)) {
            // No value or single value - show default help text and mark as not having multiple values
            resetHelpText(helpText);
            if (helpText) helpText.dataset.hasMultipleValues = '';
            return;
        }

        // Split by separator and filter empty values
        const values = [];
        for (const part of value.split(metadataValueSeparator)) {
            const trimmed = part.trim();
            if (trimmed) values.push(trimmed);
        }
        
        if (values.length > 1) {
            // Mark as having multiple values (prevents hiding on blur)
            if (helpText) {
                helpText.dataset.hasMultipleValues = 'true';
                // Update help text to show primary value with ellipsis support using span
                helpText.innerHTML = `Primary ${fieldType} is <span class="has-text-success has-text-weight-bold is-text-ellipsis" style="max-width: 150px; display: inline-block; vertical-align: middle;">${values[0]}</span>`;
            }
        } else {
            // Single value - show default help text
            resetHelpText(helpText);
            if (helpText) helpText.dataset.hasMultipleValues = '';
        }
    }

    /**
     * Reset the help text to its default state.
     * @param {HTMLElement} helpText The help text element
     */
    function resetHelpText(helpText) {
        if (helpText) {
            helpText.innerHTML = `Use <span class="tag is-link separator-char">${metadataValueSeparator}</span> to separate entries. <span class="has-text-success has-text-weight-bold">First one is primary</span>.`;
        }
    }
});