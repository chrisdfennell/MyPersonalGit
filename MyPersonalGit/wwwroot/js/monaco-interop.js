/**
 * Monaco Editor interop for Blazor Server.
 * Dynamically loads Monaco Editor from jsdelivr CDN and exposes
 * window.monacoInterop for Blazor JS interop calls.
 */
(function () {
    'use strict';

    // Internal state
    var _editors = {};
    var _models = {};
    var _originalContent = {};
    var _changeCallbacks = {};
    var _editorCounter = 0;
    var _monacoReady = false;
    var _initPromise = null;
    var _themeObserver = null;

    // Extension to Monaco language ID mapping
    var _extensionMap = {
        '.cs': 'csharp',
        '.js': 'javascript',
        '.jsx': 'javascript',
        '.ts': 'typescript',
        '.tsx': 'typescript',
        '.py': 'python',
        '.json': 'json',
        '.md': 'markdown',
        '.xml': 'xml',
        '.html': 'html',
        '.htm': 'html',
        '.css': 'css',
        '.scss': 'scss',
        '.less': 'less',
        '.yml': 'yaml',
        '.yaml': 'yaml',
        '.sh': 'shell',
        '.bash': 'shell',
        '.sql': 'sql',
        '.rs': 'rust',
        '.go': 'go',
        '.java': 'java',
        '.rb': 'ruby',
        '.php': 'php',
        '.cpp': 'cpp',
        '.cc': 'cpp',
        '.cxx': 'cpp',
        '.h': 'cpp',
        '.hpp': 'cpp',
        '.c': 'c',
        '.swift': 'swift',
        '.kt': 'kotlin',
        '.kts': 'kotlin',
        '.r': 'r',
        '.lua': 'lua',
        '.ps1': 'powershell',
        '.psm1': 'powershell',
        '.dockerfile': 'dockerfile',
        '.razor': 'razor',
        '.cshtml': 'razor',
        '.ini': 'ini',
        '.toml': 'ini',
        '.bat': 'bat',
        '.cmd': 'bat',
        '.graphql': 'graphql',
        '.proto': 'protobuf',
        '.svg': 'xml',
        '.csproj': 'xml',
        '.sln': 'plaintext',
        '.gitignore': 'plaintext',
        '.env': 'plaintext'
    };

    /**
     * Detect Monaco language ID from a file path.
     */
    function detectLanguage(filePath) {
        if (!filePath) return undefined;

        var fileName = filePath.split('/').pop().split('\\').pop().toLowerCase();

        // Special file names
        if (fileName === 'dockerfile') return 'dockerfile';
        if (fileName === 'makefile' || fileName === 'gnumakefile') return 'plaintext';

        var dotIndex = fileName.lastIndexOf('.');
        if (dotIndex === -1) return undefined;

        var ext = fileName.substring(dotIndex);
        return _extensionMap[ext] || undefined;
    }

    /**
     * Get the current theme preference from the page.
     */
    function getCurrentTheme() {
        var bsTheme = document.documentElement.getAttribute('data-bs-theme');
        if (bsTheme === 'dark') return 'vs-dark';
        return 'vs';
    }

    /**
     * Set up a MutationObserver to watch for theme changes on <html>.
     */
    function setupThemeObserver() {
        if (_themeObserver) return;

        _themeObserver = new MutationObserver(function (mutations) {
            for (var i = 0; i < mutations.length; i++) {
                if (mutations[i].attributeName === 'data-bs-theme') {
                    var newTheme = getCurrentTheme();
                    if (_monacoReady && window.monaco) {
                        monaco.editor.setTheme(newTheme);
                    }
                    break;
                }
            }
        });

        _themeObserver.observe(document.documentElement, {
            attributes: true,
            attributeFilter: ['data-bs-theme']
        });
    }

    /**
     * Create a Monaco URI for a file path.
     */
    function filePathToUri(filePath) {
        // Normalize to forward slashes and create a URI
        var normalized = filePath.replace(/\\/g, '/');
        if (!normalized.startsWith('/')) {
            normalized = '/' + normalized;
        }
        return monaco.Uri.parse('file://' + normalized);
    }

    /**
     * Simple debounce utility.
     */
    function debounce(fn, delay) {
        var timer = null;
        return function () {
            var context = this;
            var args = arguments;
            if (timer) clearTimeout(timer);
            timer = setTimeout(function () {
                timer = null;
                fn.apply(context, args);
            }, delay);
        };
    }

    window.monacoInterop = {

        /**
         * Dynamically load Monaco Editor from CDN.
         * Returns a promise that resolves when Monaco is ready.
         */
        init: function () {
            if (_initPromise) return _initPromise;

            _initPromise = new Promise(function (resolve, reject) {
                if (_monacoReady && window.monaco) {
                    resolve();
                    return;
                }

                // Check if the AMD loader is already present
                if (typeof window.require !== 'undefined' && typeof window.require.config === 'function') {
                    configureAndLoad(resolve, reject);
                    return;
                }

                // Dynamically load the AMD loader script
                var loaderScript = document.createElement('script');
                loaderScript.src = 'https://cdn.jsdelivr.net/npm/monaco-editor@0.52.2/min/vs/loader.min.js';
                loaderScript.onload = function () {
                    configureAndLoad(resolve, reject);
                };
                loaderScript.onerror = function () {
                    _initPromise = null;
                    reject(new Error('Failed to load Monaco AMD loader from CDN.'));
                };
                document.head.appendChild(loaderScript);
            });

            return _initPromise;

            function configureAndLoad(resolve, reject) {
                require.config({
                    paths: {
                        'vs': 'https://cdn.jsdelivr.net/npm/monaco-editor@0.52.2/min/vs'
                    }
                });

                // Ensure cross-origin workers function via a proxy
                window.MonacoEnvironment = {
                    getWorkerUrl: function () {
                        return 'data:text/javascript;charset=utf-8,' +
                            encodeURIComponent(
                                'self.MonacoEnvironment = { baseUrl: "https://cdn.jsdelivr.net/npm/monaco-editor@0.52.2/min/" };' +
                                'importScripts("https://cdn.jsdelivr.net/npm/monaco-editor@0.52.2/min/vs/base/worker/workerMain.js");'
                            );
                    }
                };

                require(['vs/editor/editor.main'], function () {
                    _monacoReady = true;

                    // Set the initial theme based on current page theme
                    monaco.editor.setTheme(getCurrentTheme());

                    // Watch for future theme changes
                    setupThemeObserver();

                    resolve();
                }, function (err) {
                    _initPromise = null;
                    reject(err);
                });
            }
        },

        /**
         * Create a Monaco editor in the specified container element.
         * @param {string} containerId - DOM element ID for the editor container.
         * @param {object} options - Editor options.
         * @returns {string} Editor ID.
         */
        createEditor: function (containerId, options) {
            options = options || {};

            var container = document.getElementById(containerId);
            if (!container) {
                throw new Error('Container element not found: ' + containerId);
            }

            _editorCounter++;
            var editorId = 'editor_' + _editorCounter;

            var monacoTheme = 'vs';
            if (options.theme === 'vs-dark' || options.theme === 'dark') {
                monacoTheme = 'vs-dark';
            } else if (options.theme === 'vs' || options.theme === 'light') {
                monacoTheme = 'vs';
            } else {
                // Auto-detect from page theme
                monacoTheme = getCurrentTheme();
            }

            var editor = monaco.editor.create(container, {
                theme: monacoTheme,
                fontSize: options.fontSize || 14,
                minimap: { enabled: options.minimap !== false },
                wordWrap: options.wordWrap || 'off',
                automaticLayout: true,
                scrollBeyondLastLine: false,
                renderWhitespace: 'selection',
                bracketPairColorization: { enabled: true },
                guides: { bracketPairs: true },
                smoothScrolling: true,
                cursorBlinking: 'smooth',
                cursorSmoothCaretAnimation: 'on',
                padding: { top: 8 }
            });

            _editors[editorId] = editor;
            return editorId;
        },

        /**
         * Open a file in the editor.
         * @param {string} editorId - The editor instance ID.
         * @param {string} filePath - File path used as model key.
         * @param {string} content - File content.
         * @param {boolean} isReadOnly - Whether the file should be read-only.
         */
        openFile: function (editorId, filePath, content, isReadOnly) {
            var editor = _editors[editorId];
            if (!editor) {
                throw new Error('Editor not found: ' + editorId);
            }

            var model = _models[filePath];

            if (!model || model.isDisposed()) {
                var uri = filePathToUri(filePath);
                var language = detectLanguage(filePath);

                // If we can't detect language, let Monaco infer from the URI extension
                if (language) {
                    model = monaco.editor.createModel(content, language, uri);
                } else {
                    model = monaco.editor.createModel(content, undefined, uri);
                }

                _models[filePath] = model;
                _originalContent[filePath] = content;
            }

            editor.setModel(model);
            editor.updateOptions({ readOnly: !!isReadOnly });
        },

        /**
         * Get the current active model's content.
         * @param {string} editorId - The editor instance ID.
         * @returns {string|null} The content, or null if no model is active.
         */
        getContent: function (editorId) {
            var editor = _editors[editorId];
            if (!editor) return null;

            var model = editor.getModel();
            if (!model) return null;

            return model.getValue();
        },

        /**
         * Get the content of a specific file's model.
         * @param {string} editorId - The editor instance ID (unused but kept for API consistency).
         * @param {string} filePath - The file path.
         * @returns {string|null} The content, or null if model not found.
         */
        getFileContent: function (editorId, filePath) {
            var model = _models[filePath];
            if (!model || model.isDisposed()) return null;
            return model.getValue();
        },

        /**
         * Close a file by disposing its model.
         * @param {string} editorId - The editor instance ID.
         * @param {string} filePath - The file path to close.
         */
        closeFile: function (editorId, filePath) {
            var model = _models[filePath];
            if (model && !model.isDisposed()) {
                // If this model is the active one, detach it first
                var editor = _editors[editorId];
                if (editor && editor.getModel() === model) {
                    editor.setModel(null);
                }
                model.dispose();
            }
            delete _models[filePath];
            delete _originalContent[filePath];
        },

        /**
         * Set the Monaco theme.
         * @param {string} themeName - 'light', 'dark', 'vs', or 'vs-dark'.
         */
        setTheme: function (themeName) {
            var monacoTheme;
            if (themeName === 'dark' || themeName === 'vs-dark') {
                monacoTheme = 'vs-dark';
            } else {
                monacoTheme = 'vs';
            }
            if (_monacoReady && window.monaco) {
                monaco.editor.setTheme(monacoTheme);
            }
        },

        /**
         * Recalculate editor layout dimensions.
         * @param {string} editorId - The editor instance ID.
         */
        layout: function (editorId) {
            var editor = _editors[editorId];
            if (editor) {
                editor.layout();
            }
        },

        /**
         * Register a debounced content change callback that invokes a .NET method.
         * @param {string} editorId - The editor instance ID.
         * @param {object} dotNetObjRef - .NET object reference for callback.
         * @param {string} methodName - .NET method name to invoke.
         */
        onDidChangeContent: function (editorId, dotNetObjRef, methodName) {
            var editor = _editors[editorId];
            if (!editor) {
                throw new Error('Editor not found: ' + editorId);
            }

            // Dispose previous callback if any
            if (_changeCallbacks[editorId]) {
                _changeCallbacks[editorId].dispose();
            }

            var debouncedNotify = debounce(function (filePath) {
                try {
                    dotNetObjRef.invokeMethodAsync(methodName, filePath);
                } catch (e) {
                    // .NET object reference may have been disposed
                    console.warn('Monaco interop: failed to invoke .NET callback', e);
                }
            }, 300);

            var disposable = editor.onDidChangeModelContent(function () {
                var model = editor.getModel();
                if (!model) return;

                // Find the file path for this model
                var filePath = null;
                var uri = model.uri.toString();
                for (var key in _models) {
                    if (_models[key] && !_models[key].isDisposed() && _models[key].uri.toString() === uri) {
                        filePath = key;
                        break;
                    }
                }

                debouncedNotify(filePath);
            });

            _changeCallbacks[editorId] = disposable;
        },

        /**
         * Get array of file paths whose models have been modified.
         * @param {string} editorId - The editor instance ID (unused but kept for API consistency).
         * @returns {string[]} Array of modified file paths.
         */
        getModifiedFiles: function (editorId) {
            var modified = [];
            for (var filePath in _originalContent) {
                var model = _models[filePath];
                if (model && !model.isDisposed()) {
                    if (model.getValue() !== _originalContent[filePath]) {
                        modified.push(filePath);
                    }
                }
            }
            return modified;
        },

        /**
         * Dispose an editor and all its associated models.
         * @param {string} editorId - The editor instance ID.
         */
        dispose: function (editorId) {
            // Dispose change callback
            if (_changeCallbacks[editorId]) {
                _changeCallbacks[editorId].dispose();
                delete _changeCallbacks[editorId];
            }

            // Dispose the editor
            var editor = _editors[editorId];
            if (editor) {
                editor.dispose();
                delete _editors[editorId];
            }

            // Dispose all models associated with this editor
            // Note: models are shared, so only dispose if no other editors reference them
            var hasOtherEditors = Object.keys(_editors).length > 0;
            if (!hasOtherEditors) {
                for (var filePath in _models) {
                    var model = _models[filePath];
                    if (model && !model.isDisposed()) {
                        model.dispose();
                    }
                }
                _models = {};
                _originalContent = {};
            }
        },

        /**
         * Set error/warning markers on a file's model.
         * @param {string} editorId - The editor instance ID.
         * @param {string} filePath - The file path.
         * @param {Array} markers - Array of marker objects: { startLineNumber, startColumn, endLineNumber, endColumn, message, severity }.
         *   severity: 1=Hint, 2=Info, 4=Warning, 8=Error
         */
        setModelMarkers: function (editorId, filePath, markers) {
            var model = _models[filePath];
            if (!model || model.isDisposed()) return;

            var monacoMarkers = (markers || []).map(function (m) {
                return {
                    startLineNumber: m.startLineNumber || 1,
                    startColumn: m.startColumn || 1,
                    endLineNumber: m.endLineNumber || m.startLineNumber || 1,
                    endColumn: m.endColumn || 1,
                    message: m.message || '',
                    severity: m.severity || monaco.MarkerSeverity.Error
                };
            });

            monaco.editor.setModelMarkers(model, 'blazor', monacoMarkers);
        },

        /**
         * Scroll to and reveal a specific line.
         * @param {string} editorId - The editor instance ID.
         * @param {number} lineNumber - The line number to reveal.
         */
        revealLine: function (editorId, lineNumber) {
            var editor = _editors[editorId];
            if (!editor) return;

            editor.revealLineInCenter(lineNumber);
            editor.setPosition({ lineNumber: lineNumber, column: 1 });

            // Briefly highlight the line
            var decorations = editor.deltaDecorations([], [{
                range: new monaco.Range(lineNumber, 1, lineNumber, 1),
                options: {
                    isWholeLine: true,
                    className: 'monaco-line-highlight'
                }
            }]);

            // Remove highlight after 2 seconds
            setTimeout(function () {
                editor.deltaDecorations(decorations, []);
            }, 2000);
        },

        /**
         * Focus the editor.
         * @param {string} editorId - The editor instance ID.
         */
        focus: function (editorId) {
            var editor = _editors[editorId];
            if (editor) {
                editor.focus();
            }
        }
    };
})();
