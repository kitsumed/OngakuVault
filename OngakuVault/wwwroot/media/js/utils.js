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
 * Verify and format a time value to be in the MM:SS format
 * @param {String} input The input to process
 * @returns {String} The string in a MM:SS format or null for a invalid format
 */
function verifyAndFormatTime(input) {
    // Remove all letters characters, except the colon
    input = input.replace(/[^\d:]/g, "");
    const regex = /^(\d+):(\d+\d)$/; // Allows all numbers between a ":"
    const regexResult = input.match(regex);

    if (regexResult) {
        let minutes = parseInt(regexResult[1], 10); // Minutes part
        let seconds = parseInt(regexResult[2], 10); // Seconds part

        if (seconds >= 60) {
            minutes += Math.floor(seconds / 60); // Add 1 minute for every 60 seconds
            seconds = seconds % 60; // Keep the remaining seconds (less than 60)
        }

        // Format the seconds to always have 2 numbers (00)
        const formattedSeconds = String(seconds).padStart(2, '0');
        return `${minutes}:${formattedSeconds}`;
    } else {
        // If the input are only numbers and there is no ":", treat the value as seconds
        if (input !== "" && !input.includes(":")) {
            let seconds = parseInt(input, 10);
            let minutes = Math.floor(seconds / 60); // Calculate minutes
            seconds = seconds % 60; // Get remaining seconds
            const formattedSeconds = String(seconds).padStart(2, '0');
            return `${minutes}:${formattedSeconds}`;
        }
        return null;
    }
}

/**
 * Converts a time in MM:SS format to a timestamp in milliseconds
 * @param {String} time The time in MM:SS format (ex : "2:40")
 * @returns {Number} The time in milliseconds, or null if the format is invalid
 */
function convertToMilliseconds(time) {
    // Verify if the input time matches the MM:SS format
    const regex = /^(\d+):(\d{2})$/; // MM can any numbers, SS is limited to 2 numbers
    const match = time.match(regex);

    if (match) {
        const minutes = parseInt(match[1], 10);
        const seconds = parseInt(match[2], 10);

        // Convert to to ms
        const totalSeconds = (minutes * 60) + seconds;
        const milliseconds = totalSeconds * 1000;

        return milliseconds;
    }
    return null;
}
