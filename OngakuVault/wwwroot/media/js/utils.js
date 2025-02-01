/* utils.js is intended to be loaded on all pages.
    Functions on this file are not dependent of other JS files,
    and does not have any hard-coded DOM elements.
*/

/**
 * Verify if your string is a valid url with the protocol http or https.
 * @param {String} stringUrl The string you want to check
 * @returns {Boolean} True if it's a valid url, else false
 */
function isUrlValid(stringUrl) {
    try {
        const url = new URL(stringUrl);
        return (url.protocol === "http:" || url.protocol === "https:");
    } catch (ex) {
        return false;
    }
}

/**
 * Play a animation of animate.css on a specific element
 * @param {Element} element The element to animate 
 * @param {String} animation The name of the animation
 * @returns {Promise}
 */
function animateCSS(element, animation) {
    // We create a Promise and return it
    return new Promise((resolve, reject) => {
        const animationName = `animate__${animation}`;
        if (element === null) {
            reject(new Error(`The element cannot be null.`));
        }

        element.classList.add(`animate__animated`, animationName);

        // When the animation ends, we clean the classes and resolve the Promise
        function handleAnimationEnd(event) {
            event.stopPropagation();
            element.classList.remove(`animate__animated`, animationName);
            resolve('Animation ended');
        }
        element.addEventListener('animationend', handleAnimationEnd, { once: true });
    });
}

/**
 * Will get a FormData from a formElement and convert it to json, along with converting value types.
 * By default all of FormData values are strings, regardless of the input type on DOM.
 * @param {Element} formElement The form element
 * @returns {JSON}
 */
function getFormDataAsJSON(formElement) {
    const formData = new FormData(formElement);
    const jsonObject = {};

    formData.forEach((value, key) => {
        // Get the element with the same ID as the key
        const inputElement = formElement.querySelector(`#${key}`);

        // Check if the element is of type 'number'
        if (inputElement && inputElement.type === 'number' && !isNaN(value)) {
            jsonObject[key] = Number(value);  // Convert the value to a number for the current key
        } else {
            jsonObject[key] = value;
        }
    });

    return jsonObject;
}

/**
 * Verify and format a time value to be in the MM:SS.xx format
 * @param {String} input The input to process
 * @returns {String} The string in a MM:SS.xx format or null for a invalid format
 */
function verifyAndFormatTime(input) {
    input = input.replace(/[^\d:.]/g, ""); // Match everything that is not a number, except ":" & ".", then remove them
    const regex = /^(\d+):(\d+)\.(\d+)$/; // Allows all numbers in format [MM:SS.xx]
    const regexMatch = input.match(regex);

    if (regexMatch) {
        let minutes = parseInt(regexMatch[1], 10); // Minutes part
        let seconds = parseInt(regexMatch[2], 10); // Seconds part
        let milliseconds = parseInt(regexMatch[3], 10); // Milliseconds part

        if (milliseconds >= 100) { // (0.00) > 0.99s (0.99) > 1s (1.00)
            seconds += Math.floor(milliseconds / 100); // Add 1 second for every 100 milliseconds (1,000)
            milliseconds = milliseconds % 100;  // Remove milliseconds that are part of the seconds
        }

        if (seconds >= 60) {
            minutes += Math.floor(seconds / 60); // Add 1 minute for every 60 seconds
            seconds = seconds % 60;  // Remove seconds that are part of a minute
        }

        // Format all values to always have 2 numbers (00) (minimum)
        const formattedMilliseconds = String(milliseconds).padEnd(2, '0');
        const formattedSeconds = String(seconds).padStart(2, '0');
        const formattedMinutes = String(minutes).padStart(2, '0');
        return `${formattedMinutes}:${formattedSeconds}.${formattedMilliseconds}`;
    } else {
        // If the input is not empty, try to handle it (as we already removed every non-number)
        if (input !== "") {
            let seconds = 0;
            let minutes = 0;
            let milliseconds = 0;
            // Ensure "." is removed, no millisegonds in Minutes & Seconds
            const inputMinutesAndSeconds = input.replace(".","").split(":") // Separate the : since before [0] = minutes and after [1] = segonds. (MM:SS)
            if (inputMinutesAndSeconds.length >= 2) { // Is input in format (MM:SS)
                // MINUTES
                if (inputMinutesAndSeconds[0]) { // Ensure it's not empty (input is :20)
                    minutes = parseInt(inputMinutesAndSeconds[0], 10);
                }

                // SECONDS
                if (inputMinutesAndSeconds[1]) // Ensure it's not empty (input is 20:)
                {
                    seconds = parseInt(inputMinutesAndSeconds[1], 10);
                }
            } else {
                const inputSecondsAndMilliseconds = input.split(".") // Separate the : since before [0] = segonds and after [1] = millisegonds. (SS.xx)
                if (inputSecondsAndMilliseconds.length >= 2) { // Is input in format (SS.xx)
                    // SECONDS
                    if (inputSecondsAndMilliseconds[0]) { // Ensure it's not empty (input is 20.)
                        seconds = parseInt(inputSecondsAndMilliseconds[0], 10);
                    }

                    // MILLISECONDS
                    if (inputSecondsAndMilliseconds[1]) // Ensure it's not empty (input is .20)
                    {
                        milliseconds = parseInt(inputSecondsAndMilliseconds[1], 10);
                    }
                } else { // Consider the input as beeing only segonds
                    input = input.replace(":", "")
                    input = input.replace(".", "")
                    seconds = parseInt(input, 10);
                }
            }
            // MILLISECONDS > SECONDS
            seconds += Math.floor(milliseconds / 100); // Add 1 second for every 100 milliseconds (1,000)
            milliseconds = milliseconds % 100;  // Remove milliseconds that are part of the seconds
            // SECONDS > MINUTES
            minutes += Math.floor(seconds / 60); // Calculate additional minutes from seconds
            seconds = seconds % 60; // Remove seconds that where changed to a minute

            const formattedMilliseconds = String(milliseconds).padEnd(2, '0');
            const formattedSeconds = String(seconds).padStart(2, '0');
            const formattedMinutes = String(minutes).padStart(2, '0');
            return `${formattedMinutes}:${formattedSeconds}.${formattedMilliseconds}`;
        }
        return null;
    }
}

/**
 * Converts a time in MM:SS.xx format to a timestamp in milliseconds
 * @param {String} time The time in MM:SS.xx format (ex : "2:40.00")
 * @returns {Number} The time in milliseconds, or null if the format is invalid
 */
function convertStringFormatToMilliseconds(time) {
    // Verify if the input time matches the MM:SS.xx format
    const regex = /^(\d+):(\d{2})\.(\d{2})$/; // MM support infinite numbers, SS is limited to 2 numbers, xx is limited to 2
    const match = time.match(regex);

    if (match) {
        const matchMinutes = parseInt(match[1], 10);
        const matchSeconds = parseInt(match[2], 10);
        const matchMilliseconds = parseInt(match[3], 10);

        // Convert to ms
        const totalSeconds = (matchMinutes * 60) + matchSeconds;
        let milliseconds = totalSeconds * 1000;
        /* Since we only accept 2 numbers as millisegonds (regex), we multiply by 10 to add one 0, as millisegonds
        usually are in the format ,000. Thus we turn our ",20" to a ",200"
        */
        milliseconds += matchMilliseconds * 10 

        return milliseconds;
    }
    return null;
}

/**
 * Convert milliseconds to a MM:SS.xx string format
 * @param {Number} milliseconds
 * @returns {String} A string in MM:SS.xx format
 */
function convertMillisecondsToStringFormat(milliseconds)
{
    let seconds = Math.floor(milliseconds / 1000); // +1 seconds per 1000 milliseconds
    milliseconds = milliseconds % 1000;  // Remove milliseconds that are part of the seconds
    let minutes = Math.floor(seconds / 60); // +1 minute per 60 seconds
    seconds = seconds % 60;  // Remove seconds that are part of the minutes
    // Format value to our MM:SS.xx format
    const formattedMilliseconds = String(milliseconds).padEnd(2, '0').substr(0,2);
    const formattedSeconds = String(seconds).padStart(2, '0');
    const formattedMinutes = String(minutes).padStart(2, '0');
    return `${formattedMinutes}:${formattedSeconds}.${formattedMilliseconds}`;
}