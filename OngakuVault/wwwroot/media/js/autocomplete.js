// Dynamic autocomplete functionality for directory structure based on OUTPUT_SUB_DIRECTORY_FORMAT schema
// Requires: index.js (for APIEndpoint)

class DirectoryAutocomplete {
    constructor() {
        this.isEnabled = false;
        this.schema = [];
        this.fieldMappings = {};
        this.currentSuggestions = {};
        this.debounceTimers = {};
        
        // Client-side caching
        this.suggestionCache = new Map(); // Cache for API responses
        this.failedQueries = new Map(); // Track queries that returned no results with context (depth + parentPath -> failed prefix)
        this.validatedPrimaryValues = new Map(); // Store validated primary values for multi-value fields (fieldId -> validatedValue)
        
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

    /**
     * Get the primary (first) value from an input field, handling multi-value fields.
     * @param {HTMLInputElement} input The input element
     * @returns {string} The primary value (trimmed)
     */
    getPrimaryValueFromInput(input) {
        if (!input) return '';
        const fullValue = input.value;
        const separator = typeof metadataValueSeparator !== 'undefined' ? metadataValueSeparator : ';';
        const isMultiValueField = input.dataset.multiValueField === 'true';
        
        if (isMultiValueField && fullValue.includes(separator)) {
            return fullValue.split(separator)[0].trim();
        }
        return fullValue.trim();
    }

    async initialize() {
        try {
            // Check if directory suggestions are enabled
            const enabledResponse = await fetch(`${APIEndpoint}/directory/enabled`);
            if (!enabledResponse.ok) {
                console.debug('Directory autocomplete feature disabled. API not available.');
                return;
            }

            this.isEnabled = await enabledResponse.json();
            if (!this.isEnabled) {
                console.debug('Directory autocomplete feature is disabled per server-side request.');
                return;
            }

            // Get the directory schema
            const schemaResponse = await fetch(`${APIEndpoint}/directory/schema`);
            if (schemaResponse.ok) {
                this.schema = await schemaResponse.json();
                if (this.schema.length === 0) {
                    console.debug('Directory autocomplete feature disabled. No valid tokens in schema.');
                    return;
                }

                console.debug('Directory autocomplete enabled with schema:', this.schema);
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
        // For repeated tokens, use the first occurrence for field mapping
        this.fieldMappings = {};
        
        this.schema.forEach((token, index) => {
            const fieldId = this.tokenToFieldMapping[token];
            if (fieldId && !this.fieldMappings[fieldId]) { // Only use first occurrence
                this.fieldMappings[fieldId] = {
                    token: token,
                    depth: index,
                    fieldId: fieldId
                };
            }
        });

        console.debug('Directory autocomplete field mappings established:', this.fieldMappings);
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
                // Mark that we've added listeners in the element attributes to avoid duplicates
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
                    suggestionsContainer.className = 'dropdown-suggestions-content';
                    suggestionsContainer.style.display = 'none';
                    input.parentNode.appendChild(suggestionsContainer);
                }
                
                // Create the match indicator icon (hidden by default)
                const matchIndicatorId = `${fieldId}-match-indicator`;
                if (!document.getElementById(matchIndicatorId)) {
                    const matchIndicator = document.createElement('span');
                    matchIndicator.id = matchIndicatorId;
                    matchIndicator.className = 'icon is-small is-right';
                    matchIndicator.style.display = 'none';
                    matchIndicator.style.pointerEvents = 'none';
                    
                    // Use img element with the uploaded SVG file
                    const checkImg = document.createElement('img');
                    checkImg.src = '/media/pictures/icons/white-check.svg';
                    checkImg.alt = 'Match';
                    checkImg.style.width = '2em';
                    checkImg.style.height = '2em';
                    checkImg.style.filter = 'brightness(0) saturate(100%) invert(77%) sepia(61%) saturate(425%) hue-rotate(95deg) brightness(88%) contrast(87%)'; // Makes white SVG appear as green (#48c78e)
                    matchIndicator.appendChild(checkImg);
                    
                    input.parentNode.appendChild(matchIndicator);
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
        const fullValue = event.target.value;
        const separator = typeof metadataValueSeparator !== 'undefined' ? metadataValueSeparator : ';';
        
        // Check if this is a multi-value field (artist or genre)
        const isMultiValueField = event.target.dataset.multiValueField === 'true';
        
        // Determine the value to use for autocomplete
        let value;
        if (isMultiValueField && fullValue.includes(separator)) {
            // For multi-value fields, get the text after the last separator for autocomplete
            const parts = fullValue.split(separator);
            value = parts[parts.length - 1].trim();
        } else {
            value = fullValue.trim();
        }
        
        // If field is completely empty, clear related cache for this field, validated values and hide match indicator
        if (fullValue.length === 0) {
            this.clearFieldCache(fieldId);
            this.validatedPrimaryValues.delete(fieldId);
            this.hideSuggestions(fieldId);
            this.updateMatchIndicator(fieldId, false);
            return;
        }
        
        // For multi-value fields, check if primary value has changed - if so, clear validated value
        if (isMultiValueField) {
            const primaryValue = this.getPrimaryValueFromInput(event.target);
            const validatedValue = this.validatedPrimaryValues.get(fieldId);
            if (validatedValue && validatedValue.toLowerCase() !== primaryValue.toLowerCase()) {
                // Primary value changed, clear validated value
                this.validatedPrimaryValues.delete(fieldId);
            }
            // Update match indicator based on validated primary value
            const isValid = this.isPrimaryValueValid(fieldId);
            this.updateMatchIndicator(fieldId, isValid);
        }
        
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
        const fullValue = event.target.value;
        const separator = typeof metadataValueSeparator !== 'undefined' ? metadataValueSeparator : ';';
        const isMultiValueField = event.target.dataset.multiValueField === 'true';
        
        // Determine the value to use for autocomplete
        let value;
        if (isMultiValueField && fullValue.includes(separator)) {
            // For multi-value fields, get the text after the last separator for autocomplete
            const parts = fullValue.split(separator);
            value = parts[parts.length - 1].trim();
        } else {
            value = fullValue.trim();
        }
        
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
                event.stopImmediatePropagation(); // Stop further event propagation
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
            // For multi-value fields (artist, genre), use only the PRIMARY (first) value
            let parentPath = '';
            const parentParts = [];
            
            for (let i = 0; i < mapping.depth; i++) {
                const parentToken = this.schema[i];
                const parentFieldId = Object.keys(this.fieldMappings).find(id => 
                    this.fieldMappings[id].depth === i && this.fieldMappings[id].token === parentToken
                );
                
                if (parentFieldId) {
                    const parentInput = document.getElementById(parentFieldId);
                    // Use getPrimaryValueFromInput to handle multi-value fields
                    const primaryValue = this.getPrimaryValueFromInput(parentInput);
                    if (primaryValue) {
                        parentParts.push(primaryValue);
                    } else {
                        // If a parent field is empty, we can't provide contextual suggestions
                        console.debug(`Parent field ${parentFieldId} is empty, cannot provide suggestions for ${fieldId}`);
                        this.hideSuggestions(fieldId);
                        return;
                    }
                }
            }

            if (parentParts.length > 0) {
                parentPath = parentParts.join('/');
            }

            // Create cache key for this request
            const cacheKey = JSON.stringify({
                depth: mapping.depth,
                parentPath: parentPath || null,
                filter: filter
            });

            // Check failed queries with improved logic
            const contextKey = `${mapping.depth}:${parentPath || 'null'}`;
            const failedPrefix = this.failedQueries.get(contextKey);
            
            if (failedPrefix && filter.startsWith(failedPrefix) && filter.length >= failedPrefix.length) {
                console.debug(`Query blocked due to failed prefix "${failedPrefix}" for filter "${filter}"`);
                this.hideSuggestions(fieldId);
                return;
            }

            // If user is backspacing (making filter shorter), clear failed query for this context
            if (failedPrefix && filter.length < failedPrefix.length) {
                this.failedQueries.delete(contextKey);
                console.debug(`Cleared failed prefix "${failedPrefix}" due to shorter filter "${filter}"`);
            }

            // Check cache first
            if (this.suggestionCache.has(cacheKey)) {
                console.debug(`Using cached suggestions for:`, cacheKey);
                const cachedSuggestions = this.suggestionCache.get(cacheKey);
                this.displaySuggestions(fieldId, cachedSuggestions, mapping.depth);
                return;
            }

            // Make API request for suggestions
            const request = {
                depth: mapping.depth,
                parentPath: parentPath || null,
                filter: filter
            };

            const response = await fetch(`${APIEndpoint}/directory/suggestions`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(request)
            });

            if (response.status === 204) {
                // No suggestions available - mark as failed query with the exact filter that failed
                console.debug(`No suggestions found, marking prefix "${filter}" as failed for context : ${contextKey}`);
                this.failedQueries.set(contextKey, filter);
                this.hideSuggestions(fieldId);
                this.updateMatchIndicator(fieldId, false);
                return;
            }

            if (!response.ok) {
                console.error('Failed to fetch suggestions:', response.status);
                return;
            }

            const suggestions = await response.json();
            
            // Cache the successful response
            this.suggestionCache.set(cacheKey, suggestions);
            console.debug(`Cached suggestions for: ${cacheKey}`);
            
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

        // Store suggestions for this field for later validation
        this.currentSuggestions[fieldId] = suggestionsData;

        // Get the suggestions returned by the API
        const suggestions = Array.isArray(suggestionsData) ? suggestionsData : [];
        if (suggestions.length === 0) {
            this.hideSuggestions(fieldId);
            return;
        }

        // Check for exact match with current input value
        // For multi-value fields, only check the PRIMARY (first) value
        const input = document.getElementById(fieldId);
        const primaryValue = this.getPrimaryValueFromInput(input);
        const isMultiValueField = input && input.dataset.multiValueField === 'true';
        
        const hasExactMatch = suggestions.some(s => s.name.toLowerCase() === primaryValue.toLowerCase());
        
        // Store validated primary value if it matches
        if (hasExactMatch && isMultiValueField) {
            this.validatedPrimaryValues.set(fieldId, primaryValue);
        }
        
        // Update match indicator - check if current primary matches validated value
        const isValid = this.isPrimaryValueValid(fieldId);
        this.updateMatchIndicator(fieldId, isValid);

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
                // For multi-value fields, append the suggestion after the separator
                const input = document.getElementById(fieldId);
                const currentValue = input.value;
                const separator = typeof metadataValueSeparator !== 'undefined' ? metadataValueSeparator : ';';
                const isMultiValueField = input.dataset.multiValueField === 'true';
                
                if (isMultiValueField && currentValue.includes(separator)) {
                    // Replace only the last part (after the last separator)
                    const parts = currentValue.split(separator);
                    parts[parts.length - 1] = ' ' + suggestion.name;
                    input.value = parts.join(separator);
                } else {
                    input.value = suggestion.name;
                    // Store the selected suggestion as validated primary value
                    if (isMultiValueField) {
                        this.validatedPrimaryValues.set(fieldId, suggestion.name);
                    }
                }
                
                this.hideSuggestions(fieldId);
                
                // Update match indicator based on validated primary value
                const isValid = this.isPrimaryValueValid(fieldId);
                this.updateMatchIndicator(fieldId, isValid);
                
                // Clear suggestions for subsequent fields as the context has changed
                this.clearSubsequentFields(fieldId);
            });

            suggestionsContainer.appendChild(item);
        });

        suggestionsContainer.style.display = 'block';
    }

    /**
     * Check if the primary value for a field is valid (matches a validated suggestion).
     * For multi-value fields, this checks if the primary (first) value matches a previously validated value.
     * @param {string} fieldId The field ID to check
     * @returns {boolean} True if the primary value is valid
     */
    isPrimaryValueValid(fieldId) {
        const input = document.getElementById(fieldId);
        if (!input) return false;
        
        const isMultiValueField = input.dataset.multiValueField === 'true';
        const primaryValue = this.getPrimaryValueFromInput(input);
        
        if (!primaryValue) return false;
        
        // For multi-value fields, check against validated primary values
        if (isMultiValueField) {
            const validatedValue = this.validatedPrimaryValues.get(fieldId);
            if (validatedValue && validatedValue.toLowerCase() === primaryValue.toLowerCase()) {
                return true;
            }
        }
        
        // Also check against current suggestions if available
        const suggestions = this.currentSuggestions[fieldId];
        if (suggestions && Array.isArray(suggestions)) {
            return suggestions.some(s => s.name.toLowerCase() === primaryValue.toLowerCase());
        }
        
        return false;
    }

    updateMatchIndicator(fieldId, showMatch) {
        const input = document.getElementById(fieldId);
        const matchIndicator = document.getElementById(`${fieldId}-match-indicator`);
        const inputControlElement = input.parentElement

        if (showMatch) {
            // Only if it was previsouly hidden
            if (matchIndicator.style.display === 'none') {
                // Add has-icons-right class to the input (control) div for proper Bulma icon positioning
                inputControlElement.classList.add('has-icons-right');
                // Make visible and animate
                matchIndicator.style.display = '';
                animateCSS(matchIndicator, 'bounceIn');
            }
        } else {
            // Hide the match indicator
            matchIndicator.style.display = 'none';
            inputControlElement.classList.remove('has-icons-right');
        }
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

        // Clear cache entries that may be affected by this change
        this.clearRelatedCache(changedMapping.depth);
    }

    clearRelatedCache(changedDepth) {
        // Remove cache entries for deeper levels that may be affected
        const keysToRemove = [];
        for (const [key, value] of this.suggestionCache) {
            const request = JSON.parse(key);
            if (request.depth > changedDepth) {
                keysToRemove.push(key);
            }
        }
        
        keysToRemove.forEach(key => {
            this.suggestionCache.delete(key);
        });

        // Clear failed queries for deeper levels
        const failedKeysToRemove = [];
        for (const [contextKey, failedPrefix] of this.failedQueries) {
            const [depth, parentPath] = contextKey.split(':', 2);
            if (parseInt(depth) > changedDepth) {
                failedKeysToRemove.push(contextKey);
            }
        }
        failedKeysToRemove.forEach(key => this.failedQueries.delete(key));

        if (keysToRemove.length > 0) {
            console.debug(`Cleared ${keysToRemove.length} cache entries due to context change at depth ${changedDepth}`);
        }
    }

    clearFieldCache(fieldId) {
        const mapping = this.fieldMappings[fieldId];
        if (!mapping) return;
        
        // Clear successful cache entries for this field (all filters for this depth + context)
        const keysToRemove = [];
        for (const [key, value] of this.suggestionCache) {
            const request = JSON.parse(key);
            if (request.depth === mapping.depth) {
                keysToRemove.push(key);
            }
        }
        
        keysToRemove.forEach(key => this.suggestionCache.delete(key));
        
        // Clear failed queries for this field context
        const contextKey = `${mapping.depth}:${this.getParentPath(fieldId)}`;
        this.failedQueries.delete(contextKey);
        
        if (keysToRemove.length > 0) {
            console.debug(`Cleared ${keysToRemove.length} cache entries for field ${fieldId} on empty input`);
        }
    }

    getParentPath(fieldId) {
        const mapping = this.fieldMappings[fieldId];
        if (!mapping) return null;
        
        // Build the parent path context from previous fields in the schema
        const parentParts = [];
        
        for (let i = 0; i < mapping.depth; i++) {
            const parentToken = this.schema[i];
            const parentFieldId = Object.keys(this.fieldMappings).find(id => 
                this.fieldMappings[id].depth === i && this.fieldMappings[id].token === parentToken
            );
            
            if (parentFieldId) {
                const parentInput = document.getElementById(parentFieldId);
                if (parentInput && parentInput.value.trim()) {
                    parentParts.push(parentInput.value.trim());
                } else {
                    return null; // If any parent is empty, context is invalid
                }
            }
        }
        
        return parentParts.length > 0 ? parentParts.join('/') : null;
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