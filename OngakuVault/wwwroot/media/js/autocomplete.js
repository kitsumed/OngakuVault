// Dynamic autocomplete functionality for directory structure based on OUTPUT_SUB_DIRECTORY_FORMAT schema
// Requires: index.js (for APIEndpoint)

class DirectoryAutocomplete {
    constructor() {
        this.isEnabled = false;
        this.schema = [];
        this.fieldMappings = {};
        this.currentSuggestions = {};
        this.debounceTimers = {};
        
        // Mapping from audio tokens to form field IDs
        this.tokenToFieldMapping = {
            'AUDIO_TITLE': 'name',
            'AUDIO_ARTIST': 'artistName', 
            'AUDIO_ALBUM': 'albumName',
            'AUDIO_YEAR': 'releaseYear',
            'AUDIO_TRACK_NUMBER': 'trackNumber',
            'AUDIO_GENRE': 'genre',
            'AUDIO_COMPOSER': null, // No field available
            'AUDIO_LANGUAGE': null, // No field available
            'AUDIO_DISC_NUMBER': null, // No field available
            'AUDIO_DURATION': null, // No field available
            'AUDIO_DURATION_MS': null, // No field available
            'AUDIO_ISRC': null, // No field available
            'AUDIO_CATALOG_NUMBER': null // No field available
        };
    }

    async initialize() {
        try {
            // Check if directory suggestions are enabled
            const enabledResponse = await fetch(`${APIEndpoint}/Directory/enabled`);
            if (!enabledResponse.ok) {
                console.log('Directory autocomplete feature disabled - API not available');
                return;
            }

            this.isEnabled = await enabledResponse.json();
            if (!this.isEnabled) {
                console.log('Directory autocomplete feature disabled - OUTPUT_SUB_DIRECTORY_FORMAT not configured');
                return;
            }

            // Get the directory schema
            const schemaResponse = await fetch(`${APIEndpoint}/Directory/schema`);
            if (schemaResponse.ok) {
                this.schema = await schemaResponse.json();
                if (this.schema.length === 0) {
                    console.log('Directory autocomplete feature disabled - no valid tokens in schema');
                    return;
                }

                console.log('Directory autocomplete enabled with schema:', this.schema);
                this.setupFieldMappings();
                this.setupEventListeners();
                this.observeModalChanges();
            }
        } catch (error) {
            console.error('Failed to initialize directory autocomplete:', error);
        }
    }

    setupFieldMappings() {
        // Create mappings for schema tokens that have corresponding form fields
        this.fieldMappings = {};
        
        this.schema.forEach((token, index) => {
            const fieldId = this.tokenToFieldMapping[token];
            if (fieldId) {
                this.fieldMappings[fieldId] = {
                    token: token,
                    depth: index,
                    fieldId: fieldId
                };
            }
        });

        console.log('Field mappings established:', this.fieldMappings);
    }

    observeModalChanges() {
        // Use MutationObserver to watch for when the modal is opened and re-attach event listeners
        const observer = new MutationObserver((mutations) => {
            mutations.forEach((mutation) => {
                if (mutation.type === 'attributes' && mutation.attributeName === 'class') {
                    const target = mutation.target;
                    if (target.classList.contains('is-active') || target.getAttribute('aria-hidden') === 'false') {
                        // Modal was opened, re-attach event listeners
                        setTimeout(() => {
                            this.setupEventListeners();
                        }, 100);
                    }
                }
            });
        });

        // Observe changes to modal elements
        const modals = document.querySelectorAll('.modal, [role="dialog"]');
        modals.forEach(modal => {
            observer.observe(modal, { attributes: true });
        });

        // Also observe the document for new modals being added
        observer.observe(document.body, { childList: true, subtree: true });
    }

    setupEventListeners() {
        // Set up event listeners for each field that has a mapping
        Object.keys(this.fieldMappings).forEach(fieldId => {
            const input = document.getElementById(fieldId);
            if (input && !input._autocompleteListeners) {
                // Mark that we've added listeners to avoid duplicates
                input._autocompleteListeners = true;
                
                input.addEventListener('input', (e) => this.handleInput(e, fieldId));
                input.addEventListener('focus', (e) => this.handleFocus(e, fieldId));
                input.addEventListener('blur', (e) => this.handleBlur(e, fieldId));
                input.addEventListener('keydown', (e) => this.handleKeydown(e, fieldId));
                
                // Add autocomplete attribute and suggestion container if not exists
                input.setAttribute('autocomplete', 'off');
                
                const suggestionsId = `${fieldId}-suggestions`;
                let suggestionsContainer = document.getElementById(suggestionsId);
                if (!suggestionsContainer) {
                    suggestionsContainer = document.createElement('div');
                    suggestionsContainer.id = suggestionsId;
                    suggestionsContainer.className = 'dropdown-content';
                    suggestionsContainer.style.display = 'none';
                    input.parentNode.appendChild(suggestionsContainer);
                }
                
                console.log(`Attached autocomplete listeners to field: ${fieldId} (token: ${this.fieldMappings[fieldId].token})`);
            }
        });

        // Close suggestions when clicking outside
        if (!document._autocompleteDocumentListener) {
            document._autocompleteDocumentListener = true;
            document.addEventListener('click', (e) => {
                if (!e.target.closest('.field')) {
                    this.hideAllSuggestions();
                }
            });
        }
    }

    handleInput(event, fieldId) {
        const value = event.target.value.trim();
        
        // Clear existing debounce timer
        if (this.debounceTimers[fieldId]) {
            clearTimeout(this.debounceTimers[fieldId]);
        }

        // Debounce the API call
        this.debounceTimers[fieldId] = setTimeout(() => {
            if (value.length >= 1) {
                this.showSuggestions(fieldId, value);
            } else {
                this.hideSuggestions(fieldId);
            }
        }, 300);
    }

    handleFocus(event, fieldId) {
        const value = event.target.value.trim();
        if (value.length >= 1) {
            this.showSuggestions(fieldId, value);
        }
    }

    handleBlur(event, fieldId) {
        // Hide suggestions after a short delay to allow for clicking on suggestions
        setTimeout(() => {
            this.hideSuggestions(fieldId);
        }, 150);
    }

    handleKeydown(event, fieldId) {
        const suggestionsContainer = document.getElementById(`${fieldId}-suggestions`);
        
        if (!suggestionsContainer || suggestionsContainer.style.display === 'none') {
            return;
        }

        const suggestions = suggestionsContainer.querySelectorAll('.suggestion-item');
        let currentSelected = suggestionsContainer.querySelector('.suggestion-item.selected');
        let selectedIndex = currentSelected ? Array.from(suggestions).indexOf(currentSelected) : -1;

        switch (event.key) {
            case 'ArrowDown':
                event.preventDefault();
                if (currentSelected) currentSelected.classList.remove('selected');
                selectedIndex = (selectedIndex + 1) % suggestions.length;
                suggestions[selectedIndex].classList.add('selected');
                suggestions[selectedIndex].scrollIntoView({ block: 'nearest' });
                break;

            case 'ArrowUp':
                event.preventDefault();
                if (currentSelected) currentSelected.classList.remove('selected');
                selectedIndex = selectedIndex <= 0 ? suggestions.length - 1 : selectedIndex - 1;
                suggestions[selectedIndex].classList.add('selected');
                suggestions[selectedIndex].scrollIntoView({ block: 'nearest' });
                break;

            case 'Enter':
                event.preventDefault();
                if (currentSelected) {
                    currentSelected.click();
                }
                break;

            case 'Escape':
                this.hideSuggestions(fieldId);
                break;
        }
    }

    async showSuggestions(fieldId, filter) {
        try {
            const mapping = this.fieldMappings[fieldId];
            if (!mapping) {
                return;
            }

            // Build the parent path context from previous fields in the schema
            let parentPath = '';
            const parentParts = [];
            
            for (let i = 0; i < mapping.depth; i++) {
                const parentToken = this.schema[i];
                const parentFieldId = Object.keys(this.fieldMappings).find(id => 
                    this.fieldMappings[id].token === parentToken
                );
                
                if (parentFieldId) {
                    const parentInput = document.getElementById(parentFieldId);
                    if (parentInput && parentInput.value.trim()) {
                        parentParts.push(parentInput.value.trim());
                    } else {
                        // If a parent field is empty, we can't provide contextual suggestions
                        console.log(`Parent field ${parentFieldId} is empty, cannot provide suggestions for ${fieldId}`);
                        this.hideSuggestions(fieldId);
                        return;
                    }
                }
            }

            if (parentParts.length > 0) {
                parentPath = parentParts.join('/');
            }

            // Make API request for suggestions
            const request = {
                depth: mapping.depth,
                parentPath: parentPath || null,
                filter: filter
            };

            const response = await fetch(`${APIEndpoint}/Directory/suggestions`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(request)
            });

            if (response.status === 204) {
                // No suggestions available
                this.hideSuggestions(fieldId);
                return;
            }

            if (!response.ok) {
                console.error('Failed to fetch suggestions:', response.status);
                return;
            }

            const suggestions = await response.json();
            this.displaySuggestions(fieldId, suggestions, mapping.depth);

        } catch (error) {
            console.error('Error fetching suggestions for field:', fieldId, error);
        }
    }

    displaySuggestions(fieldId, suggestionsData, depth) {
        const suggestionsContainer = document.getElementById(`${fieldId}-suggestions`);
        if (!suggestionsContainer) {
            return;
        }

        suggestionsContainer.innerHTML = '';

        const suggestions = suggestionsData.suggestions[depth] || [];
        if (suggestions.length === 0) {
            this.hideSuggestions(fieldId);
            return;
        }

        suggestions.forEach(suggestion => {
            const item = document.createElement('div');
            item.className = 'suggestion-item';
            item.textContent = suggestion.name;
            
            item.addEventListener('mouseenter', () => {
                // Remove selected class from all items
                suggestionsContainer.querySelectorAll('.suggestion-item').forEach(i => i.classList.remove('selected'));
                item.classList.add('selected');
            });

            item.addEventListener('click', () => {
                document.getElementById(fieldId).value = suggestion.name;
                this.hideSuggestions(fieldId);
                
                // Clear suggestions for subsequent fields as the context has changed
                this.clearSubsequentFields(fieldId);
            });

            suggestionsContainer.appendChild(item);
        });

        suggestionsContainer.style.display = 'block';
    }

    clearSubsequentFields(changedFieldId) {
        const changedMapping = this.fieldMappings[changedFieldId];
        if (!changedMapping) return;

        // Clear suggestions for fields that come after this one in the schema
        Object.keys(this.fieldMappings).forEach(fieldId => {
            const mapping = this.fieldMappings[fieldId];
            if (mapping.depth > changedMapping.depth) {
                this.hideSuggestions(fieldId);
            }
        });
    }

    hideSuggestions(fieldId) {
        const suggestionsContainer = document.getElementById(`${fieldId}-suggestions`);
        if (suggestionsContainer) {
            suggestionsContainer.style.display = 'none';
        }
    }

    hideAllSuggestions() {
        Object.keys(this.fieldMappings).forEach(fieldId => {
            this.hideSuggestions(fieldId);
        });
    }
}

// Global instance
let directoryAutocomplete = new DirectoryAutocomplete();

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    directoryAutocomplete.initialize();
});