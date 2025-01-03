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