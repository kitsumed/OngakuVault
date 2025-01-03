/* bulma.js is intended to be loaded on all pages of the website.
    This files has some soft dependences on DOM elements / classes,
    it is mainly related to handling bulma classes or UI animation on
    the html file.
    Requires: utils.js
*/
// Some parts of the code are based on the bulma docs
document.addEventListener('DOMContentLoaded', () => {
    // Add a click event to the DOM. 
    document.addEventListener('click', (event) => {
        // If the user interact with specific child elements of a modal, we try to close the modal
        const shouldModalElementBeClosed = event.target.matches('.modal-background, .modal-close, .modal-card-head .delete, [close-modal]');
        if (shouldModalElementBeClosed) {
            // From the element the user interacted with, get the closes element with "modal" class
            const currentModalElement = event.target.closest('.modal');
            if (currentModalElement) {
                // Verify if modal interaction on the UI are locked or not
                if (currentModalElement.matches("[user-locked-modal]")) {
                    console.log(`Cannot close modal ${currentModalElement.id}, user inputs are locked.`);
                } else {
                    closeModal(currentModalElement);
                }
            }
        }
    });

    // Add a keyboard event to close all modals
    document.addEventListener('keydown', (event) => {
        if (event.key === "Escape") {
            closeAllModals();
        }
    });
});

/**
 * Make a modal visible
 * @param {Element} modalElement The modal element
 */
function openModal(modalElement) {
    const modalCard = modalElement.querySelector(".modal-card");
    const modalBackground = modalElement.querySelector(".modal-background");

    animateCSS(modalCard, "fadeInDown")
    animateCSS(modalBackground, "fadeIn")
    modalElement.classList.add('is-active');

    // Find all buttons inside modalCard with the "is-loading" class
    const loadingButtons = modalCard.querySelectorAll('button.is-loading');
    loadingButtons.forEach(button => {
        // Ensure no buttons in the modal are in a "loading" state when first showing up
        button.classList.remove("is-loading");
    });
}

/**
 * Make a modal visible and return a promise that return with user interaction. 
 * * This method does not close the modal upon recieiving a click on the "confirm-modal" button.
 * * If the resolve is True, you need to free the modal by manually closing it once done.
 * @param {Element} modalElement The modal element
 * @returns {Promise} Resolves with true if the button with "confirm-modal" is pressed.
 */
function openModalQuestion(modalElement) {
    openModal(modalElement);
    return new Promise((resolve) => {
        // Function to handle button clicks inside the modal
        const handleButtonClick = (event) => {
            // If the button has a confirm-modal attribute
            if (event.target.matches('[confirm-modal]')) {
                // This attribute will prevent the modal from beeing closed by user inputs,
                // like pressing a close button or the escape key
                modalElement.setAttribute("user-locked-modal", "true")
                event.target.classList.add("is-loading");
                resolveModalQuestion(true);
            }
        };
        // Add event listener
        modalElement.addEventListener('click', handleButtonClick);

        // Create a MutationObserver to watch for the removal of 'is-active'
        const modalObserver = new MutationObserver((mutationsList) => {
            for (const mutation of mutationsList) {
                if (mutation.type === 'attributes' && mutation.attributeName === 'class') {
                    // Verify if the 'is-active' class is removed
                    if (!modalElement.classList.contains('is-active')) {
                        // User closed the modal
                        resolveModalQuestion(false);
                    }
                }
            }
        });

        // Start observing the modal element
        modalObserver.observe(modalElement, {
            attributes: true, // Observe changes to attributes
            attributeFilter: ['class'] // Only listen for class attribute changes
        });

        // Function to disconnect all events and return the promise response
        const resolveModalQuestion = (boolStatus) => {
            modalObserver.disconnect(); // Stop observing for class updates
            modalElement.removeEventListener('click', handleButtonClick); // Stop event listener for click on modal
            resolve(boolStatus);
        };
    });
}

/**
 * Make a modal invisible. Include locked modals (unlock them)
 * @param {Element} modalElement The modal element
 */
function closeModal(modalElement) {
    const modalCard = modalElement.querySelector(".modal-card");
    const modalBackground = modalElement.querySelector(".modal-background");
    animateCSS(modalBackground, "fadeOut")
    animateCSS(modalCard, "fadeOutDown").then(() => {
        modalElement.classList.remove('is-active');
        // Remove the UI locked attribute if found
        modalElement.removeAttribute("user-locked-modal");
    });
}

/**
 * Make all non-locked modal invisible
 */
function closeAllModals() {
    const modals = document.querySelectorAll('.modal');
    if (modals.length > 0) {
        modals.forEach((modal) => {
            // Verify if modal interaction on the UI are locked or not
            if (modal.matches("[user-locked-modal]")) {
                console.log(`Cannot close modal ${modal.id}, user inputs are locked.`);
            } else {
                closeModal(modal);
            }
        });
    }
}