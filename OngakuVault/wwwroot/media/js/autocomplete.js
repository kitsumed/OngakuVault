// Autocomplete functionality for artist and album names
// Requires: index.js (for APIEndpoint)

class DirectoryAutocomplete {
    constructor() {
        this.isEnabled = false;
        this.suggestionCache = null;
        this.cacheTimestamp = 0;
        this.cacheExpiry = 5 * 60 * 1000; // 5 minutes in milliseconds
        this.currentArtistFilter = '';
        this.currentAlbumFilter = '';
        this.debounceTimers = {};
    }

    async initialize() {
        try {
            // Check if directory suggestions are enabled
            const response = await fetch(`${APIEndpoint}/Directory/enabled`);
            if (response.ok) {
                this.isEnabled = await response.json();
                if (this.isEnabled) {
                    this.setupEventListeners();
                    this.observeModalChanges();
                    console.log('Directory autocomplete feature enabled');
                } else {
                    console.log('Directory autocomplete feature disabled - OUTPUT_SUB_DIRECTORY_FORMAT not configured');
                }
            }
        } catch (error) {
            console.error('Failed to check if directory suggestions are enabled:', error);
        }
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
        const artistInput = document.getElementById('artistName');
        const albumInput = document.getElementById('albumName');

        if (artistInput && !artistInput._autocompleteListeners) {
            // Mark that we've added listeners to avoid duplicates
            artistInput._autocompleteListeners = true;
            
            artistInput.addEventListener('input', (e) => this.handleArtistInput(e));
            artistInput.addEventListener('focus', (e) => this.handleArtistFocus(e));
            artistInput.addEventListener('blur', (e) => this.handleBlur(e));
            artistInput.addEventListener('keydown', (e) => this.handleKeydown(e));
            
            console.log('Attached autocomplete listeners to artist input');
        }

        if (albumInput && !albumInput._autocompleteListeners) {
            // Mark that we've added listeners to avoid duplicates
            albumInput._autocompleteListeners = true;
            
            albumInput.addEventListener('input', (e) => this.handleAlbumInput(e));
            albumInput.addEventListener('focus', (e) => this.handleAlbumFocus(e));
            albumInput.addEventListener('blur', (e) => this.handleBlur(e));
            albumInput.addEventListener('keydown', (e) => this.handleKeydown(e));
            
            console.log('Attached autocomplete listeners to album input');
        }

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

    handleArtistInput(event) {
        const value = event.target.value.trim();
        this.currentArtistFilter = value;
        
        // Clear existing debounce timer
        if (this.debounceTimers.artist) {
            clearTimeout(this.debounceTimers.artist);
        }

        // Debounce the API call
        this.debounceTimers.artist = setTimeout(() => {
            if (value.length >= 1) {
                this.showArtistSuggestions(value);
            } else {
                this.hideSuggestions('artistName');
            }
        }, 300);
    }

    handleArtistFocus(event) {
        const value = event.target.value.trim();
        if (value.length >= 1) {
            this.showArtistSuggestions(value);
        }
    }

    handleAlbumInput(event) {
        const value = event.target.value.trim();
        this.currentAlbumFilter = value;
        
        // Clear existing debounce timer
        if (this.debounceTimers.album) {
            clearTimeout(this.debounceTimers.album);
        }

        // Debounce the API call
        this.debounceTimers.album = setTimeout(() => {
            if (value.length >= 1) {
                this.showAlbumSuggestions(value);
            } else {
                this.hideSuggestions('albumName');
            }
        }, 300);
    }

    handleAlbumFocus(event) {
        const value = event.target.value.trim();
        if (value.length >= 1) {
            this.showAlbumSuggestions(value);
        }
    }

    handleBlur(event) {
        // Hide suggestions after a short delay to allow for clicking on suggestions
        setTimeout(() => {
            const fieldId = event.target.id;
            this.hideSuggestions(fieldId);
        }, 150);
    }

    handleKeydown(event) {
        const fieldId = event.target.id;
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

    async getSuggestions(artistFilter = null, albumFilter = null) {
        try {
            // Check cache first
            const now = Date.now();
            if (this.suggestionCache && (now - this.cacheTimestamp) < this.cacheExpiry) {
                return this.suggestionCache;
            }

            // Build query parameters
            const params = new URLSearchParams();
            if (artistFilter) params.append('artistFilter', artistFilter);
            if (albumFilter) params.append('albumFilter', albumFilter);

            const response = await fetch(`${APIEndpoint}/Directory/suggestions?${params.toString()}`);
            
            if (response.status === 204) {
                // No content - no suggestions available
                return null;
            }
            
            if (!response.ok) {
                console.error('Failed to fetch directory suggestions:', response.status, response.statusText);
                return null;
            }

            const suggestions = await response.json();
            
            // Cache the results only if no filters were applied
            if (!artistFilter && !albumFilter) {
                this.suggestionCache = suggestions;
                this.cacheTimestamp = now;
            }

            return suggestions;
        } catch (error) {
            console.error('Error fetching directory suggestions:', error);
            return null;
        }
    }

    async showArtistSuggestions(filter) {
        const suggestions = await this.getSuggestions(filter, null);
        if (!suggestions || !suggestions.artists || suggestions.artists.length === 0) {
            this.hideSuggestions('artistName');
            return;
        }

        const suggestionsContainer = document.getElementById('artistName-suggestions');
        suggestionsContainer.innerHTML = '';

        suggestions.artists.forEach(artist => {
            const item = document.createElement('div');
            item.className = 'suggestion-item';
            item.textContent = artist;
            
            item.addEventListener('mouseenter', () => {
                // Remove selected class from all items
                suggestionsContainer.querySelectorAll('.suggestion-item').forEach(i => i.classList.remove('selected'));
                item.classList.add('selected');
            });

            item.addEventListener('click', () => {
                document.getElementById('artistName').value = artist;
                this.hideSuggestions('artistName');
                // Trigger album suggestions update if there's an album filter
                const albumInput = document.getElementById('albumName');
                if (albumInput && albumInput.value.trim()) {
                    this.showAlbumSuggestions(albumInput.value.trim());
                }
            });

            suggestionsContainer.appendChild(item);
        });

        suggestionsContainer.style.display = 'block';
    }

    async showAlbumSuggestions(filter) {
        const artistName = document.getElementById('artistName').value.trim();
        const suggestions = await this.getSuggestions(artistName || null, filter);
        
        if (!suggestions || !suggestions.albums) {
            this.hideSuggestions('albumName');
            return;
        }

        const suggestionsContainer = document.getElementById('albumName-suggestions');
        suggestionsContainer.innerHTML = '';

        let albumsToShow = [];

        // If we have a specific artist selected, show only that artist's albums
        if (artistName && suggestions.albums[artistName]) {
            albumsToShow = suggestions.albums[artistName].filter(album => 
                album.toLowerCase().includes(filter.toLowerCase())
            );
        } else {
            // Show all albums that match the filter
            Object.values(suggestions.albums).forEach(artistAlbums => {
                artistAlbums.forEach(album => {
                    if (album.toLowerCase().includes(filter.toLowerCase()) && !albumsToShow.includes(album)) {
                        albumsToShow.push(album);
                    }
                });
            });
        }

        if (albumsToShow.length === 0) {
            this.hideSuggestions('albumName');
            return;
        }

        albumsToShow.forEach(album => {
            const item = document.createElement('div');
            item.className = 'suggestion-item';
            item.textContent = album;
            
            item.addEventListener('mouseenter', () => {
                // Remove selected class from all items
                suggestionsContainer.querySelectorAll('.suggestion-item').forEach(i => i.classList.remove('selected'));
                item.classList.add('selected');
            });

            item.addEventListener('click', () => {
                document.getElementById('albumName').value = album;
                this.hideSuggestions('albumName');
            });

            suggestionsContainer.appendChild(item);
        });

        suggestionsContainer.style.display = 'block';
    }

    hideSuggestions(fieldId) {
        const suggestionsContainer = document.getElementById(`${fieldId}-suggestions`);
        if (suggestionsContainer) {
            suggestionsContainer.style.display = 'none';
        }
    }

    hideAllSuggestions() {
        this.hideSuggestions('artistName');
        this.hideSuggestions('albumName');
    }
}

// Global instance
let directoryAutocomplete = new DirectoryAutocomplete();

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    directoryAutocomplete.initialize();
});