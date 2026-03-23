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

    /**
     * Resolve a single merge conflict by replacing the conflict block.
     * @param {object} editor - Monaco editor instance.
     * @param {object} conflict - Conflict range object.
     * @param {string} resolution - 'current', 'incoming', or 'both'.
     */
    function resolveConflict(editor, conflict, resolution) {
        var model = editor.getModel();
        if (!model) return;

        var currentLines = [];
        for (var i = conflict.currentStart; i <= conflict.currentEnd; i++) {
            currentLines.push(model.getLineContent(i));
        }

        var incomingLines = [];
        for (var i = conflict.incomingStart; i <= conflict.incomingEnd; i++) {
            incomingLines.push(model.getLineContent(i));
        }

        var replacement;
        if (resolution === 'current') {
            replacement = currentLines.join('\n');
        } else if (resolution === 'incoming') {
            replacement = incomingLines.join('\n');
        } else {
            replacement = currentLines.join('\n') + '\n' + incomingLines.join('\n');
        }

        var range = new monaco.Range(
            conflict.markerStart, 1,
            conflict.markerEnd, model.getLineMaxColumn(conflict.markerEnd)
        );

        editor.executeEdits('conflict-resolve', [{
            range: range,
            text: replacement
        }]);

        // Re-detect remaining conflicts after a short delay
        setTimeout(function () {
            window.monacoInterop.detectConflicts(
                Object.keys(_editors).find(function (k) { return _editors[k] === editor; }),
                null
            );
        }, 100);
    }

    /**
     * Load Emmet abbreviation support for HTML/CSS languages.
     * Uses emmet-monaco-es from CDN with AMD define temporarily hidden.
     */
    function loadEmmetSupport() {
        var savedDefine = window.define;
        window.define = undefined;

        var script = document.createElement('script');
        script.src = 'https://cdn.jsdelivr.net/npm/emmet-monaco-es@5.4.0/dist/emmet-monaco.min.js';
        script.onload = function () {
            window.define = savedDefine;
            try {
                if (window.emmetMonaco && window.emmetMonaco.emmetHTML) {
                    window.emmetMonaco.emmetHTML(monaco, ['html', 'razor', 'php']);
                    window.emmetMonaco.emmetCSS(monaco, ['css', 'scss', 'less']);
                    console.log('Emmet support loaded successfully.');
                }
            } catch (e) {
                console.warn('Emmet initialization error:', e);
            }
        };
        script.onerror = function () {
            window.define = savedDefine;
            console.warn('Failed to load Emmet support from CDN.');
        };
        document.head.appendChild(script);
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

                    // Load Emmet support for HTML/CSS
                    loadEmmetSupport();

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
                matchBrackets: 'always',
                guides: { bracketPairs: true, indentation: true, highlightActiveBracketPair: true, highlightActiveIndentation: true },
                folding: true,
                foldingStrategy: 'auto',
                showFoldingControls: 'always',
                stickyScroll: { enabled: false },
                smoothScrolling: true,
                cursorBlinking: 'smooth',
                cursorSmoothCaretAnimation: 'on',
                formatOnPaste: true,
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
        },

        /**
         * Update editor options at runtime (for settings panel).
         * @param {string} editorId - The editor instance ID.
         * @param {object} options - Monaco editor options to update.
         */
        updateEditorOptions: function (editorId, options) {
            var editor = _editors[editorId];
            if (editor) {
                editor.updateOptions(options);
            }
        },

        /**
         * Create a second editor in a split container alongside the main editor.
         * @param {string} containerId - The parent container for the split layout.
         * @param {string} originalEditorId - The ID of the existing editor.
         * @param {object} options - Editor creation options.
         * @returns {string} The new split editor ID.
         */
        createSplitEditor: function (containerId, originalEditorId, options) {
            options = options || {};

            var parentContainer = document.getElementById(containerId);
            if (!parentContainer) {
                throw new Error('Container element not found: ' + containerId);
            }

            // The parent should already have a split layout via CSS.
            // Look for the split container div.
            var splitContainer = document.getElementById(containerId + '-split');
            if (!splitContainer) {
                splitContainer = document.createElement('div');
                splitContainer.id = containerId + '-split';
                splitContainer.style.cssText = 'width: 100%; height: 100%;';
                parentContainer.appendChild(splitContainer);
            }

            _editorCounter++;
            var editorId = 'editor_' + _editorCounter;

            var monacoTheme = getCurrentTheme();

            var editor = monaco.editor.create(splitContainer, {
                theme: monacoTheme,
                fontSize: options.fontSize || 14,
                minimap: { enabled: options.minimap !== false },
                wordWrap: options.wordWrap || 'off',
                automaticLayout: true,
                scrollBeyondLastLine: false,
                renderWhitespace: options.renderWhitespace || 'selection',
                bracketPairColorization: { enabled: true },
                matchBrackets: 'always',
                guides: { bracketPairs: true, indentation: true, highlightActiveBracketPair: true, highlightActiveIndentation: true },
                folding: true,
                foldingStrategy: 'auto',
                showFoldingControls: 'always',
                stickyScroll: { enabled: false },
                smoothScrolling: true,
                cursorBlinking: 'smooth',
                cursorSmoothCaretAnimation: 'on',
                formatOnPaste: true,
                padding: { top: 8 }
            });

            _editors[editorId] = editor;
            return editorId;
        },

        /**
         * Close a split editor and remove its container.
         * @param {string} editorId - The split editor instance ID.
         * @param {string} containerId - The parent container ID.
         */
        closeSplitEditor: function (editorId, containerId) {
            // Dispose the editor
            var editor = _editors[editorId];
            if (editor) {
                editor.dispose();
                delete _editors[editorId];
            }

            // Dispose change callback
            if (_changeCallbacks[editorId]) {
                _changeCallbacks[editorId].dispose();
                delete _changeCallbacks[editorId];
            }

            // Remove the split container element
            var splitContainer = document.getElementById(containerId + '-split');
            if (splitContainer) {
                splitContainer.remove();
            }
        },

        /**
         * Create a Monaco diff editor.
         * @param {string} containerId - DOM element ID for the diff editor container.
         * @param {string} originalContent - The original (left side) content.
         * @param {string} modifiedContent - The modified (right side) content.
         * @param {string} filePath - The file path (for language detection).
         * @returns {string} A diff editor ID.
         */
        createDiffEditor: function (containerId, originalContent, modifiedContent, filePath) {
            var container = document.getElementById(containerId);
            if (!container) {
                throw new Error('Container element not found: ' + containerId);
            }

            _editorCounter++;
            var diffEditorId = 'diff_' + _editorCounter;

            var language = detectLanguage(filePath);
            var uri = filePathToUri(filePath);

            var originalModel = monaco.editor.createModel(originalContent, language);
            var modifiedModel = monaco.editor.createModel(modifiedContent, language);

            var diffEditor = monaco.editor.createDiffEditor(container, {
                theme: getCurrentTheme(),
                automaticLayout: true,
                readOnly: true,
                renderSideBySide: true,
                scrollBeyondLastLine: false,
                minimap: { enabled: false },
                padding: { top: 8 }
            });

            diffEditor.setModel({
                original: originalModel,
                modified: modifiedModel
            });

            // Store for cleanup — use a special prefix to distinguish from regular editors
            _editors[diffEditorId] = diffEditor;
            _models[diffEditorId + '_original'] = originalModel;
            _models[diffEditorId + '_modified'] = modifiedModel;

            return diffEditorId;
        },

        /**
         * Dispose a diff editor and its models.
         * @param {string} diffEditorId - The diff editor instance ID.
         */
        disposeDiffEditor: function (diffEditorId) {
            var editor = _editors[diffEditorId];
            if (editor) {
                editor.dispose();
                delete _editors[diffEditorId];
            }

            // Clean up the models
            var origModel = _models[diffEditorId + '_original'];
            if (origModel && !origModel.isDisposed()) origModel.dispose();
            delete _models[diffEditorId + '_original'];

            var modModel = _models[diffEditorId + '_modified'];
            if (modModel && !modModel.isDisposed()) modModel.dispose();
            delete _models[diffEditorId + '_modified'];
        },

        /**
         * Get the original (committed) content for a file.
         * @param {string} filePath - The file path.
         * @returns {string|null} The original content, or null.
         */
        getOriginalContent: function (filePath) {
            return _originalContent[filePath] || null;
        },

        /**
         * Set blame decorations on an editor.
         * @param {string} editorId - The editor instance ID.
         * @param {Array} blameData - Array of {startLine, endLine, commitSha, author, date, message}.
         */
        setBlameDecorations: function (editorId, blameData) {
            var editor = _editors[editorId];
            if (!editor) return;

            // Clear previous blame decorations
            if (editor._blameDecorations) {
                editor.removeDecorations(editor._blameDecorations);
            }

            if (!blameData || blameData.length === 0) {
                editor._blameDecorations = [];
                return;
            }

            var decorations = [];
            for (var i = 0; i < blameData.length; i++) {
                var hunk = blameData[i];
                var dateStr = new Date(hunk.date).toLocaleDateString();
                var label = hunk.commitSha + ' ' + hunk.author + ', ' + dateStr + ' \u2014 ' + hunk.message;

                // Only show annotation on the first line of each hunk
                decorations.push({
                    range: new monaco.Range(hunk.startLine, 1, hunk.startLine, 1),
                    options: {
                        isWholeLine: false,
                        before: {
                            content: ' ' + label + ' ',
                            inlineClassName: 'ide-blame-annotation',
                            cursorStops: monaco.editor.InjectedTextCursorStops.None
                        }
                    }
                });

                // Add background to all lines in the hunk (alternating colors)
                var bgClass = i % 2 === 0 ? 'ide-blame-line-even' : 'ide-blame-line-odd';
                for (var line = hunk.startLine; line <= hunk.endLine; line++) {
                    decorations.push({
                        range: new monaco.Range(line, 1, line, 1),
                        options: {
                            isWholeLine: true,
                            className: bgClass
                        }
                    });
                }
            }

            editor._blameDecorations = editor.createDecorationsCollection(decorations).getRanges ? [] :
                editor.deltaDecorations([], decorations);

            // Use the newer API if available, fallback to deltaDecorations
            try {
                var collection = editor.createDecorationsCollection(decorations);
                editor._blameDecorationsCollection = collection;
                editor._blameDecorations = [];
            } catch (e) {
                editor._blameDecorations = editor.deltaDecorations([], decorations);
            }
        },

        /**
         * Clear blame decorations from an editor.
         * @param {string} editorId - The editor instance ID.
         */
        clearBlameDecorations: function (editorId) {
            var editor = _editors[editorId];
            if (!editor) return;

            if (editor._blameDecorationsCollection) {
                editor._blameDecorationsCollection.clear();
                editor._blameDecorationsCollection = null;
            }
            if (editor._blameDecorations && editor._blameDecorations.length > 0) {
                editor.deltaDecorations(editor._blameDecorations, []);
                editor._blameDecorations = [];
            }
        },

        /**
         * Replace all occurrences in a model.
         * @param {string} filePath - The file path of the model.
         * @param {string} search - The search string.
         * @param {string} replace - The replacement string.
         * @returns {number} Number of replacements made.
         */
        replaceInModel: function (filePath, search, replace) {
            var model = _models[filePath];
            if (!model || model.isDisposed()) return 0;

            var content = model.getValue();
            var count = 0;
            var regex = new RegExp(search.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'), 'gi');
            var newContent = content.replace(regex, function () { count++; return replace; });

            if (count > 0) {
                model.setValue(newContent);
            }
            return count;
        },

        /**
         * Trigger Monaco's built-in find/replace widget.
         * @param {string} editorId - The editor instance ID.
         */
        triggerFindReplace: function (editorId) {
            var editor = _editors[editorId];
            if (editor) {
                editor.trigger('keyboard', 'editor.action.startFindReplaceAction');
            }
        },

        /**
         * Register a cursor position change callback.
         * @param {string} editorId - The editor instance ID.
         * @param {object} dotNetObjRef - .NET object reference.
         * @param {string} methodName - .NET method name to invoke with {lineNumber, column, selectionLength}.
         */
        onDidChangeCursorPosition: function (editorId, dotNetObjRef, methodName) {
            var editor = _editors[editorId];
            if (!editor) return;

            editor.onDidChangeCursorPosition(function (e) {
                var selection = editor.getSelection();
                var selectionLength = 0;
                if (selection && !selection.isEmpty()) {
                    var model = editor.getModel();
                    if (model) {
                        var text = model.getValueInRange(selection);
                        selectionLength = text.length;
                    }
                }
                try {
                    dotNetObjRef.invokeMethodAsync(methodName, e.position.lineNumber, e.position.column, selectionLength);
                } catch (ex) { }
            });
        },

        /**
         * Trigger Monaco's Go to Line dialog.
         * @param {string} editorId - The editor instance ID.
         */
        triggerGoToLine: function (editorId) {
            var editor = _editors[editorId];
            if (editor) {
                editor.trigger('keyboard', 'editor.action.gotoLine');
            }
        },

        /**
         * Toggle minimap visibility.
         * @param {string} editorId - The editor instance ID.
         * @returns {boolean} New minimap state.
         */
        toggleMinimap: function (editorId) {
            var editor = _editors[editorId];
            if (!editor) return false;
            var opts = editor.getOptions();
            // Monaco option ID for minimap.enabled is 73 in recent versions
            var current = editor.getRawOptions().minimap?.enabled !== false;
            var newVal = !current;
            editor.updateOptions({ minimap: { enabled: newVal } });
            return newVal;
        },

        /**
         * Get the language ID of the current model.
         * @param {string} editorId - The editor instance ID.
         * @returns {string|null} The language ID.
         */
        getLanguageId: function (editorId) {
            var editor = _editors[editorId];
            if (!editor) return null;
            var model = editor.getModel();
            if (!model) return null;
            return model.getLanguageId();
        },

        /**
         * Toggle word wrap and return the new state.
         * @param {string} editorId - The editor instance ID.
         * @returns {string} New word wrap state ('on' or 'off').
         */
        toggleWordWrap: function (editorId) {
            var editor = _editors[editorId];
            if (!editor) return 'off';
            var current = editor.getRawOptions().wordWrap || 'off';
            var newVal = current === 'off' ? 'on' : 'off';
            editor.updateOptions({ wordWrap: newVal });
            return newVal;
        },

        /**
         * Get document symbols (outline) for the current model.
         * Uses Monaco's built-in document symbol provider.
         * @param {string} editorId - The editor instance ID.
         * @returns {Promise<Array>} Array of symbol objects.
         */
        getDocumentSymbols: function (editorId) {
            var editor = _editors[editorId];
            if (!editor) return Promise.resolve([]);
            var model = editor.getModel();
            if (!model) return Promise.resolve([]);

            // Try to get symbols from Monaco's built-in providers
            return monaco.editor.getModel(model.uri) ?
                new Promise(function (resolve) {
                    // Use a timeout to allow language services to initialize
                    setTimeout(function () {
                        try {
                            // Access the document symbol provider
                            var symbols = [];
                            var lines = model.getLinesContent();
                            var language = model.getLanguageId();

                            // Parse symbols from code using regex patterns
                            var patterns = [];

                            if (['csharp', 'java', 'kotlin', 'typescript', 'javascript'].indexOf(language) >= 0) {
                                patterns.push({ regex: /(?:public|private|protected|internal|static|async|abstract|virtual|override|sealed|partial)?\s*(?:class|interface|struct|enum|record)\s+(\w+)/g, kind: 'class' });
                                patterns.push({ regex: /(?:public|private|protected|internal|static|async|abstract|virtual|override)?\s*(?:[\w<>\[\]?,\s]+)\s+(\w+)\s*\([^)]*\)\s*(?:\{|=>|;)/g, kind: 'method' });
                                patterns.push({ regex: /(?:public|private|protected|internal|static)?\s*(?:[\w<>\[\]?,]+)\s+(\w+)\s*\{\s*(?:get|set)/g, kind: 'property' });
                            }
                            if (['python'].indexOf(language) >= 0) {
                                patterns.push({ regex: /^class\s+(\w+)/gm, kind: 'class' });
                                patterns.push({ regex: /^(?:\s*)def\s+(\w+)/gm, kind: 'method' });
                            }
                            if (['go'].indexOf(language) >= 0) {
                                patterns.push({ regex: /^func\s+(?:\(\w+\s+\*?\w+\)\s+)?(\w+)/gm, kind: 'method' });
                                patterns.push({ regex: /^type\s+(\w+)\s+(?:struct|interface)/gm, kind: 'class' });
                            }
                            if (['rust'].indexOf(language) >= 0) {
                                patterns.push({ regex: /^(?:pub\s+)?fn\s+(\w+)/gm, kind: 'method' });
                                patterns.push({ regex: /^(?:pub\s+)?(?:struct|enum|trait)\s+(\w+)/gm, kind: 'class' });
                                patterns.push({ regex: /^(?:pub\s+)?impl(?:<[^>]*>)?\s+(\w+)/gm, kind: 'class' });
                            }
                            if (['html', 'xml', 'razor'].indexOf(language) >= 0) {
                                patterns.push({ regex: /<(h[1-6]|section|article|nav|header|footer|main|div)\b[^>]*(?:id|class)="([^"]+)"/gi, kind: 'property' });
                            }
                            if (['css', 'scss', 'less'].indexOf(language) >= 0) {
                                patterns.push({ regex: /^([.#@][\w-]+(?:\s*[,>+~]\s*[.#@]?[\w-]+)*)\s*\{/gm, kind: 'property' });
                            }
                            if (['ruby'].indexOf(language) >= 0) {
                                patterns.push({ regex: /^class\s+(\w+)/gm, kind: 'class' });
                                patterns.push({ regex: /^\s*def\s+(\w+[?!]?)/gm, kind: 'method' });
                            }
                            if (['php'].indexOf(language) >= 0) {
                                patterns.push({ regex: /(?:public|private|protected|static)?\s*function\s+(\w+)/g, kind: 'method' });
                                patterns.push({ regex: /class\s+(\w+)/g, kind: 'class' });
                            }
                            // Fallback: any language - look for common patterns
                            if (patterns.length === 0) {
                                patterns.push({ regex: /(?:function|def|fn|func|sub)\s+(\w+)/gm, kind: 'method' });
                                patterns.push({ regex: /(?:class|struct|interface|enum|type)\s+(\w+)/gm, kind: 'class' });
                            }

                            var fullText = model.getValue();
                            for (var p = 0; p < patterns.length; p++) {
                                var pattern = patterns[p];
                                var match;
                                pattern.regex.lastIndex = 0;
                                while ((match = pattern.regex.exec(fullText)) !== null) {
                                    var name = match[1] || match[2] || match[0];
                                    // Calculate line number from offset
                                    var pos = model.getPositionAt(match.index);
                                    symbols.push({
                                        name: name,
                                        kind: pattern.kind,
                                        line: pos.lineNumber,
                                        column: pos.column
                                    });
                                }
                            }

                            // Sort by line number
                            symbols.sort(function (a, b) { return a.line - b.line; });
                            resolve(symbols);
                        } catch (e) {
                            console.warn('Symbol parsing error:', e);
                            resolve([]);
                        }
                    }, 100);
                }) : Promise.resolve([]);
        },

        /**
         * Navigate to a specific position in the editor.
         * @param {string} editorId - The editor instance ID.
         * @param {number} lineNumber - Line number.
         * @param {number} column - Column number.
         */
        goToPosition: function (editorId, lineNumber, column) {
            var editor = _editors[editorId];
            if (!editor) return;
            editor.revealLineInCenter(lineNumber);
            editor.setPosition({ lineNumber: lineNumber, column: column || 1 });
            editor.focus();
        },

        /**
         * Fold all regions in the editor.
         * @param {string} editorId - The editor instance ID.
         */
        foldAll: function (editorId) {
            var editor = _editors[editorId];
            if (editor) {
                editor.trigger('keyboard', 'editor.foldAll');
            }
        },

        /**
         * Unfold all regions in the editor.
         * @param {string} editorId - The editor instance ID.
         */
        unfoldAll: function (editorId) {
            var editor = _editors[editorId];
            if (editor) {
                editor.trigger('keyboard', 'editor.unfoldAll');
            }
        },

        /**
         * Add minimap highlights for modified lines (vs original content).
         * Shows changed lines as colored markers in the overview ruler.
         * @param {string} editorId - The editor instance ID.
         */
        enableMinimapHighlights: function (editorId) {
            var editor = _editors[editorId];
            if (!editor) return;

            var updateHighlights = function () {
                var model = editor.getModel();
                if (!model) return;

                var decorations = [];

                // Find the file path for this model
                var filePath = null;
                var uri = model.uri.toString();
                for (var key in _models) {
                    if (_models[key] && !_models[key].isDisposed() && _models[key].uri.toString() === uri) {
                        filePath = key;
                        break;
                    }
                }

                // Modified/added/deleted lines (vs original) with gutter indicators
                if (filePath && _originalContent[filePath] != null) {
                    var original = _originalContent[filePath].split('\n');
                    var current = model.getLinesContent();
                    var maxLines = Math.max(original.length, current.length);
                    for (var i = 0; i < maxLines; i++) {
                        if (i >= original.length || i >= current.length || original[i] !== current[i]) {
                            var lineNum = i + 1;
                            if (lineNum <= current.length) {
                                var isAdded = i >= original.length;
                                var color = isAdded ? '#2ea04370' : '#0078d470';
                                var gutterClass = isAdded ? 'ide-gutter-added' : 'ide-gutter-modified';
                                decorations.push({
                                    range: new monaco.Range(lineNum, 1, lineNum, 1),
                                    options: {
                                        isWholeLine: true,
                                        linesDecorationsClassName: gutterClass,
                                        overviewRuler: { color: color, position: monaco.editor.OverviewRulerLane.Left },
                                        minimap: { color: color, position: monaco.editor.MinimapPosition.Gutter }
                                    }
                                });
                            }
                        }
                    }
                    // Deleted lines indicator — red triangle on the line after the deletion
                    if (original.length > current.length) {
                        var deletePoint = Math.min(original.length, current.length);
                        if (deletePoint > 0) {
                            decorations.push({
                                range: new monaco.Range(deletePoint, 1, deletePoint, 1),
                                options: {
                                    isWholeLine: false,
                                    linesDecorationsClassName: 'ide-gutter-deleted',
                                    overviewRuler: { color: '#f8514970', position: monaco.editor.OverviewRulerLane.Left }
                                }
                            });
                        }
                    }
                }

                // Conflict markers
                var text = model.getValue();
                var lines = text.split('\n');
                for (var i = 0; i < lines.length; i++) {
                    if (lines[i].startsWith('<<<<<<<') || lines[i].startsWith('=======') || lines[i].startsWith('>>>>>>>')) {
                        decorations.push({
                            range: new monaco.Range(i + 1, 1, i + 1, 1),
                            options: {
                                isWholeLine: true,
                                overviewRuler: { color: '#f8514970', position: monaco.editor.OverviewRulerLane.Full },
                                minimap: { color: '#f8514970', position: monaco.editor.MinimapPosition.Gutter }
                            }
                        });
                    }
                }

                if (editor._minimapDecorations) {
                    try { editor._minimapDecorations.clear(); } catch (e) { }
                }
                if (decorations.length > 0) {
                    editor._minimapDecorations = editor.createDecorationsCollection(decorations);
                }
            };

            updateHighlights();
            editor.onDidChangeModelContent(debounce(updateHighlights, 500));
            editor.onDidChangeModel(updateHighlights);
        },

        /**
         * Detect and decorate merge conflicts in the current model.
         * Adds clickable "Accept Current", "Accept Incoming", "Accept Both" widgets.
         * @param {string} editorId - The editor instance ID.
         * @param {object} dotNetObjRef - .NET object reference for callbacks.
         * @returns {number} Number of conflicts found.
         */
        detectConflicts: function (editorId, dotNetObjRef) {
            var editor = _editors[editorId];
            if (!editor) return 0;
            var model = editor.getModel();
            if (!model) return 0;

            // Clear previous conflict decorations
            if (editor._conflictDecorations) {
                try { editor._conflictDecorations.clear(); } catch (e) { }
            }
            if (editor._conflictWidgets) {
                editor._conflictWidgets.forEach(function (w) {
                    try { editor.removeContentWidget(w); } catch (e) { }
                });
            }
            editor._conflictWidgets = [];

            var text = model.getValue();
            var lines = text.split('\n');
            var conflicts = [];
            var i = 0;

            while (i < lines.length) {
                if (lines[i].startsWith('<<<<<<<')) {
                    var startLine = i + 1; // 1-based
                    var separatorLine = -1;
                    var endLine = -1;

                    for (var j = i + 1; j < lines.length; j++) {
                        if (lines[j].startsWith('=======')) {
                            separatorLine = j + 1;
                        } else if (lines[j].startsWith('>>>>>>>')) {
                            endLine = j + 1;
                            break;
                        }
                    }

                    if (separatorLine > 0 && endLine > 0) {
                        conflicts.push({
                            markerStart: startLine,
                            currentStart: startLine + 1,
                            currentEnd: separatorLine - 1,
                            separator: separatorLine,
                            incomingStart: separatorLine + 1,
                            incomingEnd: endLine - 1,
                            markerEnd: endLine
                        });
                        i = endLine; // skip past this conflict (0-based: endLine is 1-based)
                    } else {
                        i++;
                    }
                } else {
                    i++;
                }
            }

            if (conflicts.length === 0) return 0;

            var decorations = [];
            var widgetCounter = 0;

            conflicts.forEach(function (c) {
                // Highlight current (green)
                decorations.push({
                    range: new monaco.Range(c.markerStart, 1, c.currentEnd, model.getLineMaxColumn(c.currentEnd)),
                    options: {
                        isWholeLine: true,
                        className: 'ide-conflict-current',
                        overviewRuler: { color: '#2ea04370', position: monaco.editor.OverviewRulerLane.Full }
                    }
                });

                // Highlight incoming (blue)
                decorations.push({
                    range: new monaco.Range(c.incomingStart, 1, c.markerEnd, model.getLineMaxColumn(c.markerEnd)),
                    options: {
                        isWholeLine: true,
                        className: 'ide-conflict-incoming',
                        overviewRuler: { color: '#0078d470', position: monaco.editor.OverviewRulerLane.Full }
                    }
                });

                // Separator line
                decorations.push({
                    range: new monaco.Range(c.separator, 1, c.separator, 1),
                    options: { isWholeLine: true, className: 'ide-conflict-separator' }
                });

                // Add a content widget with action buttons above the conflict marker
                widgetCounter++;
                var widgetId = 'conflict-widget-' + widgetCounter;
                var conflict = c;

                var widget = {
                    getId: function () { return widgetId; },
                    getDomNode: function () {
                        if (this._node) return this._node;
                        var node = document.createElement('div');
                        node.className = 'ide-conflict-actions';
                        node.innerHTML =
                            '<span class="ide-conflict-btn ide-conflict-accept-current" title="Accept Current">Accept Current</span>' +
                            '<span class="ide-conflict-btn-sep">|</span>' +
                            '<span class="ide-conflict-btn ide-conflict-accept-incoming" title="Accept Incoming">Accept Incoming</span>' +
                            '<span class="ide-conflict-btn-sep">|</span>' +
                            '<span class="ide-conflict-btn ide-conflict-accept-both" title="Accept Both">Accept Both</span>';

                        var cc = conflict;
                        node.querySelector('.ide-conflict-accept-current').onclick = function () {
                            resolveConflict(editor, cc, 'current');
                            if (dotNetObjRef) {
                                try { dotNetObjRef.invokeMethodAsync('OnConflictResolved'); } catch (e) { }
                            }
                        };
                        node.querySelector('.ide-conflict-accept-incoming').onclick = function () {
                            resolveConflict(editor, cc, 'incoming');
                            if (dotNetObjRef) {
                                try { dotNetObjRef.invokeMethodAsync('OnConflictResolved'); } catch (e) { }
                            }
                        };
                        node.querySelector('.ide-conflict-accept-both').onclick = function () {
                            resolveConflict(editor, cc, 'both');
                            if (dotNetObjRef) {
                                try { dotNetObjRef.invokeMethodAsync('OnConflictResolved'); } catch (e) { }
                            }
                        };

                        this._node = node;
                        return node;
                    },
                    getPosition: function () {
                        return {
                            position: { lineNumber: conflict.markerStart, column: 1 },
                            preference: [monaco.editor.ContentWidgetPositionPreference.ABOVE]
                        };
                    }
                };

                editor.addContentWidget(widget);
                editor._conflictWidgets.push(widget);
            });

            editor._conflictDecorations = editor.createDecorationsCollection(decorations);
            return conflicts.length;
        },

        /**
         * Add inline color swatches for CSS color values.
         * @param {string} editorId - The editor instance ID.
         */
        enableColorDecorations: function (editorId) {
            var editor = _editors[editorId];
            if (!editor) return;

            var updateColors = function () {
                var model = editor.getModel();
                if (!model) return;
                var lang = model.getLanguageId();
                if (['css', 'scss', 'less', 'html', 'razor'].indexOf(lang) === -1) return;

                var text = model.getValue();
                var colorRegex = /#(?:[0-9a-fA-F]{3,4}){1,2}\b|rgba?\(\s*\d+[\s,]+\d+[\s,]+\d+(?:[\s,/]+[\d.]+%?)?\s*\)|hsla?\(\s*\d+[\s,]+[\d.]+%[\s,]+[\d.]+%(?:[\s,/]+[\d.]+%?)?\s*\)/g;

                var decorations = [];
                var match;
                while ((match = colorRegex.exec(text)) !== null) {
                    var pos = model.getPositionAt(match.index);
                    var endPos = model.getPositionAt(match.index + match[0].length);

                    // Create a unique CSS class for this color
                    var color = match[0];
                    var className = 'ide-color-' + match.index;

                    // Inject a style for this specific swatch
                    var styleId = 'color-swatch-' + editorId + '-' + match.index;
                    var existing = document.getElementById(styleId);
                    if (existing) existing.remove();
                    var style = document.createElement('style');
                    style.id = styleId;
                    style.textContent = '.' + className + '::before { content: ""; display: inline-block; width: 10px; height: 10px; margin-right: 3px; border-radius: 2px; border: 1px solid #666; background: ' + color + '; vertical-align: middle; }';
                    document.head.appendChild(style);

                    decorations.push({
                        range: new monaco.Range(pos.lineNumber, pos.column, endPos.lineNumber, endPos.column),
                        options: {
                            beforeContentClassName: className
                        }
                    });
                }

                if (editor._colorDecorations) {
                    try { editor._colorDecorations.clear(); } catch (e) { }
                }
                if (decorations.length > 0) {
                    editor._colorDecorations = editor.createDecorationsCollection(decorations);
                }
            };

            // Run on file open and on content change
            updateColors();
            editor.onDidChangeModelContent(debounce(updateColors, 500));
            editor.onDidChangeModel(updateColors);
        },

        /**
         * Register Go to Definition provider for Ctrl+Click.
         * Uses regex-based symbol search across all loaded models.
         * @param {string} editorId - The editor instance ID.
         * @param {object} dotNetObjRef - .NET object reference for cross-file navigation.
         */
        registerDefinitionProvider: function (editorId, dotNetObjRef) {
            var editor = _editors[editorId];
            if (!editor) return;

            // Register for all languages
            var languages = ['csharp', 'javascript', 'typescript', 'python', 'go', 'rust', 'java', 'ruby', 'php', 'css', 'html', 'razor'];
            languages.forEach(function (lang) {
                monaco.languages.registerDefinitionProvider(lang, {
                    provideDefinition: function (model, position) {
                        var word = model.getWordAtPosition(position);
                        if (!word) return null;
                        var symbol = word.word;
                        if (symbol.length < 2) return null;

                        // Search current model first
                        var results = [];
                        var patterns = [
                            new RegExp('(?:class|interface|struct|enum|record)\\s+' + symbol + '\\b'),
                            new RegExp('(?:function|def|fn|func|sub)\\s+' + symbol + '\\s*\\('),
                            new RegExp('(?:public|private|protected|internal|static|async|override|virtual|abstract)\\s+(?:[\\w<>\\[\\]?,\\s]+\\s+)?' + symbol + '\\s*[\\(\\{]'),
                            new RegExp('(?:const|let|var|val)\\s+' + symbol + '\\s*[=:]'),
                            new RegExp('type\\s+' + symbol + '\\s'),
                        ];

                        // Search all loaded models
                        for (var filePath in _models) {
                            var m = _models[filePath];
                            if (!m || m.isDisposed()) continue;
                            var text = m.getValue();
                            var lines = text.split('\n');
                            for (var i = 0; i < lines.length; i++) {
                                for (var p = 0; p < patterns.length; p++) {
                                    if (patterns[p].test(lines[i])) {
                                        var col = lines[i].indexOf(symbol) + 1;
                                        results.push({
                                            uri: m.uri,
                                            range: new monaco.Range(i + 1, col, i + 1, col + symbol.length)
                                        });
                                        break;
                                    }
                                }
                            }
                        }

                        // If no results in loaded models, ask .NET to search the repo
                        if (results.length === 0 && dotNetObjRef) {
                            try {
                                dotNetObjRef.invokeMethodAsync('OnGoToDefinition', symbol);
                            } catch (e) { }
                            return null;
                        }

                        // Filter out the current position (don't navigate to yourself)
                        var currentUri = model.uri.toString();
                        var currentLine = position.lineNumber;
                        results = results.filter(function (r) {
                            return !(r.uri.toString() === currentUri && r.range.startLineNumber === currentLine);
                        });

                        return results.length > 0 ? results : null;
                    }
                });
            });
        },

        /**
         * Update the stored original content for a file (after auto-save).
         * @param {string} editorId - The editor instance ID.
         * @param {string} filePath - The file path.
         * @param {string} content - The new original content.
         */
        updateOriginalContent: function (editorId, filePath, content) {
            _originalContent[filePath] = content;
        },

        /**
         * Get all LSP diagnostics across all open models.
         * @returns {Array} Array of diagnostic objects with filePath, line, column, message, severity.
         */
        getAllDiagnostics: function () {
            var results = [];
            var markers = monaco.editor.getModelMarkers({ owner: 'lsp' });
            markers.forEach(function (m) {
                var filePath = null;
                for (var key in _models) {
                    if (_models[key] && !_models[key].isDisposed() && _models[key].uri.toString() === m.resource.toString()) {
                        filePath = key;
                        break;
                    }
                }
                if (filePath) {
                    results.push({
                        filePath: filePath,
                        startLineNumber: m.startLineNumber,
                        startColumn: m.startColumn,
                        endLineNumber: m.endLineNumber,
                        endColumn: m.endColumn,
                        message: m.message,
                        severity: m.severity,
                        source: m.source || '',
                        code: m.code ? String(m.code) : ''
                    });
                }
            });
            return results;
        }
    };

    // ============================================================
    // Panel resize helper
    // ============================================================
    window.ideResize = {
        _active: null,

        startSidebarResize: function (dotNetRef, startX) {
            var sidebar = document.querySelector('.ide-sidebar');
            if (!sidebar) return;
            var startWidth = sidebar.offsetWidth;

            this._active = {
                type: 'sidebar',
                dotNetRef: dotNetRef,
                startX: startX,
                startWidth: startWidth
            };

            document.body.style.cursor = 'col-resize';
            document.body.style.userSelect = 'none';
            document.addEventListener('mousemove', this._onMouseMove);
            document.addEventListener('mouseup', this._onMouseUp);
        },

        startBottomResize: function (dotNetRef, startY) {
            var panel = document.querySelector('.ide-bottom-panel');
            if (!panel) return;
            var startHeight = panel.offsetHeight;

            this._active = {
                type: 'bottom',
                dotNetRef: dotNetRef,
                startY: startY,
                startHeight: startHeight
            };

            document.body.style.cursor = 'row-resize';
            document.body.style.userSelect = 'none';
            document.addEventListener('mousemove', this._onMouseMove);
            document.addEventListener('mouseup', this._onMouseUp);
        },

        _onMouseMove: function (e) {
            var ctx = window.ideResize._active;
            if (!ctx) return;
            e.preventDefault();

            if (ctx.type === 'sidebar') {
                var newWidth = ctx.startWidth + (e.clientX - ctx.startX);
                try { ctx.dotNetRef.invokeMethodAsync('OnSidebarResized', Math.round(newWidth)); } catch (ex) { }
            } else if (ctx.type === 'bottom') {
                var newHeight = ctx.startHeight - (e.clientY - ctx.startY);
                try { ctx.dotNetRef.invokeMethodAsync('OnBottomPanelResized', Math.round(newHeight)); } catch (ex) { }
            }
        },

        _onMouseUp: function () {
            var ctx = window.ideResize._active;
            window.ideResize._active = null;
            document.body.style.cursor = '';
            document.body.style.userSelect = '';
            document.removeEventListener('mousemove', window.ideResize._onMouseMove);
            document.removeEventListener('mouseup', window.ideResize._onMouseUp);

            if (ctx && ctx.dotNetRef) {
                try { ctx.dotNetRef.invokeMethodAsync('OnResizeEnd'); } catch (ex) { }
            }
        }
    };

    // ============================================================
    // Tab scroll helper
    // ============================================================
    window.ideTabScroll = {
        getScrollState: function (el) {
            if (!el) return { scrollLeft: 0, clientWidth: 0, scrollWidth: 0 };
            return {
                scrollLeft: el.scrollLeft,
                clientWidth: el.clientWidth,
                scrollWidth: el.scrollWidth
            };
        },
        scroll: function (el, delta) {
            if (el) {
                el.scrollBy({ left: delta, behavior: 'smooth' });
            }
        }
    };

    // ============================================================
    // Terminal (xterm.js) interop
    // ============================================================
    var _terminals = {};
    var _terminalWs = {};

    window.ideTerminal = {
        /**
         * Dynamically load xterm.js and the fit addon from CDN.
         * Temporarily hides AMD define to prevent conflicts with Monaco's loader.
         */
        loadXterm: function () {
            if (window.Terminal) return Promise.resolve();

            return new Promise(function (resolve, reject) {
                // Load CSS
                var link = document.createElement('link');
                link.rel = 'stylesheet';
                link.href = 'https://cdn.jsdelivr.net/npm/@xterm/xterm@5.5.0/css/xterm.min.css';
                document.head.appendChild(link);

                // Temporarily hide AMD define so xterm uses global export instead
                var savedDefine = window.define;
                window.define = undefined;

                // Load xterm.js
                var script = document.createElement('script');
                script.src = 'https://cdn.jsdelivr.net/npm/@xterm/xterm@5.5.0/lib/xterm.min.js';
                script.onload = function () {
                    // Load fit addon
                    var fitScript = document.createElement('script');
                    fitScript.src = 'https://cdn.jsdelivr.net/npm/@xterm/addon-fit@0.10.0/lib/addon-fit.min.js';
                    fitScript.onload = function () {
                        // Restore AMD define
                        window.define = savedDefine;
                        resolve();
                    };
                    fitScript.onerror = function () {
                        window.define = savedDefine;
                        reject(new Error('Failed to load xterm fit addon'));
                    };
                    document.head.appendChild(fitScript);
                };
                script.onerror = function () {
                    window.define = savedDefine;
                    reject(new Error('Failed to load xterm.js'));
                };
                document.head.appendChild(script);
            });
        },

        /**
         * Initialize a terminal in the given container and connect via WebSocket.
         * @param {string} containerId - The DOM element ID.
         * @param {string} wsUrl - The WebSocket URL for the terminal backend.
         */
        init: async function (containerId, wsUrl) {
            // Ensure xterm is loaded
            await window.ideTerminal.loadXterm();

            var container = document.getElementById(containerId);
            if (!container) {
                throw new Error('Terminal container not found: ' + containerId);
            }

            // Create terminal
            var term = new Terminal({
                cursorBlink: true,
                fontSize: 13,
                fontFamily: "'Cascadia Code', 'Fira Code', 'Consolas', monospace",
                theme: {
                    background: '#1e1e1e',
                    foreground: '#cccccc',
                    cursor: '#ffffff',
                    selectionBackground: '#264f78'
                },
                allowProposedApi: true
            });

            var fitAddon = new FitAddon.FitAddon();
            term.loadAddon(fitAddon);

            term.open(container);
            fitAddon.fit();

            _terminals[containerId] = { terminal: term, fitAddon: fitAddon };

            // Connect WebSocket
            try {
                var ws = new WebSocket(wsUrl);
                _terminalWs[containerId] = ws;

                ws.onopen = function () {
                    term.writeln('\x1b[32mTerminal connected.\x1b[0m\r\n');
                };

                ws.onmessage = function (event) {
                    term.write(event.data);
                };

                ws.onclose = function () {
                    term.writeln('\r\n\x1b[31mTerminal session ended.\x1b[0m');
                };

                ws.onerror = function (e) {
                    term.writeln('\r\n\x1b[31mWebSocket connection error.\x1b[0m');
                    console.error('Terminal WebSocket error:', e);
                };

                // Send user input to WebSocket
                term.onData(function (data) {
                    if (ws.readyState === WebSocket.OPEN) {
                        ws.send(data);
                    }
                });
            } catch (e) {
                term.writeln('\x1b[31mFailed to connect: ' + e.message + '\x1b[0m');
            }
        },

        /**
         * Fit the terminal to its container.
         * @param {string} containerId - The DOM element ID.
         */
        fit: function (containerId) {
            var t = _terminals[containerId];
            if (t && t.fitAddon) {
                t.fitAddon.fit();
            }
        },

        /**
         * Re-fit all active terminals to their containers.
         */
        fitAll: function () {
            for (var containerId in _terminals) {
                var t = _terminals[containerId];
                if (t && t.fitAddon) {
                    try { t.fitAddon.fit(); } catch (e) { }
                }
            }
        },

        /**
         * Dispose a terminal and close its WebSocket.
         * @param {string} containerId - The DOM element ID.
         */
        dispose: function (containerId) {
            var ws = _terminalWs[containerId];
            if (ws) {
                try { ws.close(); } catch (e) { }
                delete _terminalWs[containerId];
            }

            var t = _terminals[containerId];
            if (t && t.terminal) {
                t.terminal.dispose();
                delete _terminals[containerId];
            }
        }
    };
    // ============================================================
    // Preview helpers (markdown, unsaved changes)
    // ============================================================

    window.idePreview = {
        _markedLoaded: false,
        _markedPromise: null,

        /**
         * Load the marked.js library for markdown rendering.
         */
        loadMarked: function () {
            if (this._markedPromise) return this._markedPromise;
            if (window.marked) { this._markedLoaded = true; return Promise.resolve(); }

            var self = this;
            this._markedPromise = new Promise(function (resolve, reject) {
                var savedDefine = window.define;
                window.define = undefined;

                var script = document.createElement('script');
                script.src = 'https://cdn.jsdelivr.net/npm/marked@15.0.7/marked.min.js';
                script.onload = function () {
                    window.define = savedDefine;
                    self._markedLoaded = true;
                    resolve();
                };
                script.onerror = function () {
                    window.define = savedDefine;
                    reject(new Error('Failed to load marked.js'));
                };
                document.head.appendChild(script);
            });
            return this._markedPromise;
        },

        /**
         * Render markdown to HTML.
         * @param {string} markdown - The markdown string.
         * @returns {string} HTML string.
         */
        renderMarkdown: async function (markdown) {
            await this.loadMarked();
            if (window.marked && window.marked.parse) {
                return marked.parse(markdown || '', { breaks: true, gfm: true });
            }
            return '<pre>' + (markdown || '').replace(/</g, '&lt;') + '</pre>';
        }
    };

    // ============================================================
    // Unsaved changes warning
    // ============================================================

    window.ideUnsaved = {
        _enabled: false,

        /**
         * Enable or disable the beforeunload warning.
         * @param {boolean} hasChanges - Whether there are unsaved changes.
         */
        setHasChanges: function (hasChanges) {
            if (hasChanges && !this._enabled) {
                window.addEventListener('beforeunload', this._handler);
                this._enabled = true;
            } else if (!hasChanges && this._enabled) {
                window.removeEventListener('beforeunload', this._handler);
                this._enabled = false;
            }
        },

        _handler: function (e) {
            e.preventDefault();
            e.returnValue = '';
        }
    };

    // ============================================================
    // Drag & drop file upload helper
    // ============================================================

    window.ideUpload = {
        /**
         * Set up drop zone on an element. Calls .NET when files are dropped.
         * @param {string} elementId - The DOM element ID to attach drop events to.
         * @param {object} dotNetRef - .NET object reference.
         * @param {string} methodName - .NET method to call with {name, path, base64Content}.
         */
        setupDropZone: function (elementId, dotNetRef, methodName) {
            var el = document.getElementById(elementId);
            if (!el) return;

            el.addEventListener('dragover', function (e) {
                e.preventDefault();
                e.stopPropagation();
                el.classList.add('ide-drop-active');
            });

            el.addEventListener('dragleave', function (e) {
                e.preventDefault();
                el.classList.remove('ide-drop-active');
            });

            el.addEventListener('drop', function (e) {
                e.preventDefault();
                e.stopPropagation();
                el.classList.remove('ide-drop-active');

                var files = e.dataTransfer.files;
                if (!files || files.length === 0) return;

                for (var i = 0; i < files.length; i++) {
                    (function (file) {
                        var reader = new FileReader();
                        reader.onload = function (ev) {
                            var base64 = ev.target.result.split(',')[1] || '';
                            try {
                                dotNetRef.invokeMethodAsync(methodName, file.name, base64);
                            } catch (ex) {
                                console.error('Drop upload error:', ex);
                            }
                        };
                        reader.readAsDataURL(file);
                    })(files[i]);
                }
            });
        }
    };

    // ============================================================
    // Task runner
    // ============================================================
    window.ideTaskRunner = {
        _ws: null,
        _dotNetRef: null,

        connect: function (wsUrl, dotNetRef) {
            this._dotNetRef = dotNetRef;
            var self = this;
            return new Promise(function (resolve) {
                var ws = new WebSocket(wsUrl);
                self._ws = ws;
                ws.onopen = function () { resolve(true); };
                ws.onmessage = function (event) {
                    try {
                        var msg = JSON.parse(event.data);
                        if (dotNetRef) {
                            dotNetRef.invokeMethodAsync('OnTaskOutput', msg.type || '', msg.data || '', msg.code || 0);
                        }
                    } catch (e) { console.error('[TaskRunner] Parse error:', e); }
                };
                ws.onclose = function () {
                    if (self._ws === ws) {
                        if (dotNetRef) {
                            try { dotNetRef.invokeMethodAsync('OnTaskOutput', 'exit', '', -1); } catch (e) { }
                        }
                        self._ws = null;
                    }
                };
                ws.onerror = function () { resolve(false); };
                setTimeout(function () { resolve(false); }, 10000);
            });
        },

        run: function (command) {
            if (this._ws && this._ws.readyState === WebSocket.OPEN) {
                this._ws.send(JSON.stringify({ command: command }));
            }
        },

        cancel: function () {
            if (this._ws && this._ws.readyState === WebSocket.OPEN) {
                this._ws.send(JSON.stringify({ type: 'cancel' }));
            }
        },

        disconnect: function () {
            if (this._ws) { try { this._ws.close(); } catch (e) { } this._ws = null; }
        }
    };

    // ============================================================
    // LSP (Language Server Protocol) client
    // ============================================================
    window.ideLsp = {
        _connections: {},   // keyed by language ID
        _requestId: 0,
        _pendingRequests: {},
        _dotNetRef: null,
        _serverCapabilities: {},
        _disposables: [],   // Monaco provider disposables

        // Monaco language ID -> LSP server language key
        _languageMap: {
            'csharp': 'csharp',
            'javascript': 'typescript',
            'typescript': 'typescript',
            'python': 'python',
            'go': 'go',
            'rust': 'rust',
            'html': 'html',
            'css': 'css',
            'scss': 'css',
            'less': 'css',
            'json': 'json',
            'yaml': 'yaml',
            'shell': 'bash',
            'dockerfile': 'dockerfile',
            'markdown': 'markdown'
        },

        /**
         * Initialize LSP with the Blazor DotNetObjectReference.
         */
        init: function (dotNetRef) {
            this._dotNetRef = dotNetRef;
        },

        /**
         * Connect to a language server for a given language.
         * @param {string} language - LSP server key (csharp, typescript, python).
         * @param {string} wsUrl - WebSocket URL for the LSP relay.
         * @param {string} wsUrl - WebSocket URL for the LSP relay.
         * @returns {Promise<boolean>} Whether connection and initialization succeeded.
         */
        connect: function (language, wsUrl) {
            var self = this;

            if (this._connections[language]) {
                return Promise.resolve(true); // Already connected
            }

            return new Promise(function (resolve) {
                var ws = new WebSocket(wsUrl);
                var conn = {
                    ws: ws,
                    ready: false,
                    rootUri: null,
                    gotInit: false,
                    openDocuments: {} // uri -> version
                };

                ws.onopen = function () {
                    self._connections[language] = conn;
                    // Wait for the init message from the server with the rootUri
                };

                ws.onmessage = function (event) {
                    try {
                        var msg = JSON.parse(event.data);

                        // First message from server is the init message with rootUri
                        if (!conn.gotInit && msg.type === 'init' && msg.rootUri) {
                            conn.gotInit = true;
                            conn.rootUri = msg.rootUri;
                            console.log('[LSP] Got worktree rootUri:', msg.rootUri);

                            // Now send LSP initialize request
                            self._sendRequest(language, 'initialize', {
                                processId: null,
                                capabilities: {
                                    textDocument: {
                                        completion: {
                                            completionItem: {
                                                snippetSupport: true,
                                                commitCharactersSupport: true,
                                                documentationFormat: ['markdown', 'plaintext'],
                                                resolveSupport: { properties: ['documentation', 'detail'] }
                                            },
                                            contextSupport: true
                                        },
                                        hover: { contentFormat: ['markdown', 'plaintext'] },
                                        signatureHelp: { signatureInformation: { documentationFormat: ['markdown', 'plaintext'], parameterInformation: { labelOffsetSupport: true } } },
                                        definition: { linkSupport: false },
                                        references: {},
                                        documentSymbol: { hierarchicalDocumentSymbolSupport: true },
                                        formatting: {},
                                        rangeFormatting: {},
                                        rename: { prepareSupport: true },
                                        codeAction: { codeActionLiteralSupport: { codeActionKind: { valueSet: ['quickfix', 'refactor', 'source'] } } },
                                        publishDiagnostics: { relatedInformation: true }
                                    },
                                    workspace: {
                                        workspaceFolders: true,
                                        didChangeConfiguration: {}
                                    }
                                },
                                rootUri: conn.rootUri,
                                workspaceFolders: [{ uri: conn.rootUri, name: 'workspace' }]
                            }).then(function (result) {
                                self._serverCapabilities[language] = result.capabilities || {};
                                conn.ready = true;

                                // Send initialized notification
                                self._sendNotification(language, 'initialized', {});

                                // Register Monaco providers for this language
                                self._registerProviders(language);

                                // Notify Blazor
                                if (self._dotNetRef) {
                                    try { self._dotNetRef.invokeMethodAsync('OnLspStatusChanged', language, 'running'); } catch (e) { }
                                }

                                resolve(true);
                            }).catch(function (err) {
                                console.error('[LSP] Initialize failed:', err);
                                resolve(false);
                            });
                            return;
                        }

                        self._handleMessage(language, msg);
                    } catch (e) {
                        console.error('[LSP] Failed to parse message:', e);
                    }
                };

                ws.onerror = function () {
                    if (self._dotNetRef) {
                        try { self._dotNetRef.invokeMethodAsync('OnLspStatusChanged', language, 'error'); } catch (e) { }
                    }
                };

                ws.onclose = function () {
                    delete self._connections[language];
                    if (self._dotNetRef) {
                        try { self._dotNetRef.invokeMethodAsync('OnLspStatusChanged', language, 'disconnected'); } catch (e) { }
                    }
                };

                // Timeout after 10 seconds
                setTimeout(function () {
                    if (!conn.ready) {
                        resolve(false);
                    }
                }, 10000);
            });
        },

        /**
         * Disconnect from a language server.
         */
        disconnect: function (language) {
            var conn = this._connections[language];
            if (!conn) return;

            try {
                this._sendRequest(language, 'shutdown', null).then(function () {
                    try { conn.ws.close(); } catch (e) { }
                });
                this._sendNotification(language, 'exit', null);
            } catch (e) {
                try { conn.ws.close(); } catch (e2) { }
            }
            delete this._connections[language];
        },

        /**
         * Disconnect all language servers.
         */
        disconnectAll: function () {
            var self = this;
            // Dispose Monaco providers
            this._disposables.forEach(function (d) { try { d.dispose(); } catch (e) { } });
            this._disposables = [];

            Object.keys(this._connections).forEach(function (lang) {
                self.disconnect(lang);
            });
        },

        /**
         * Check if a language has an active LSP connection.
         */
        isConnected: function (language) {
            var lspLang = this._languageMap[language] || language;
            var conn = this._connections[lspLang];
            return !!(conn && conn.ready);
        },

        /**
         * Notify the language server that a document was opened.
         */
        didOpen: function (filePath, languageId, content) {
            var lspLang = this._languageMap[languageId] || languageId;
            var conn = this._connections[lspLang];
            if (!conn || !conn.ready) return;

            var uri = this._filePathToUri(filePath, conn.rootUri);
            if (conn.openDocuments[uri]) return; // Already open

            conn.openDocuments[uri] = 1;
            this._sendNotification(lspLang, 'textDocument/didOpen', {
                textDocument: {
                    uri: uri,
                    languageId: languageId === 'javascript' ? 'javascript' : languageId,
                    version: 1,
                    text: content
                }
            });
        },

        /**
         * Notify the language server that a document changed (full sync).
         */
        didChange: function (filePath, languageId, content) {
            var lspLang = this._languageMap[languageId] || languageId;
            var conn = this._connections[lspLang];
            if (!conn || !conn.ready) return;

            var uri = this._filePathToUri(filePath, conn.rootUri);
            var version = (conn.openDocuments[uri] || 1) + 1;
            conn.openDocuments[uri] = version;

            this._sendNotification(lspLang, 'textDocument/didChange', {
                textDocument: { uri: uri, version: version },
                contentChanges: [{ text: content }]
            });
        },

        /**
         * Notify the language server that a document was closed.
         */
        didClose: function (filePath, languageId) {
            var lspLang = this._languageMap[languageId] || languageId;
            var conn = this._connections[lspLang];
            if (!conn || !conn.ready) return;

            var uri = this._filePathToUri(filePath, conn.rootUri);
            delete conn.openDocuments[uri];

            this._sendNotification(lspLang, 'textDocument/didClose', {
                textDocument: { uri: uri }
            });
        },

        /**
         * Notify the language server that a document was saved.
         */
        didSave: function (filePath, languageId, content) {
            var lspLang = this._languageMap[languageId] || languageId;
            var conn = this._connections[lspLang];
            if (!conn || !conn.ready) return;

            var uri = this._filePathToUri(filePath, conn.rootUri);
            this._sendNotification(lspLang, 'textDocument/didSave', {
                textDocument: { uri: uri },
                text: content
            });
        },

        // ---- Internal methods ----

        _filePathToUri: function (filePath, rootUri) {
            // filePath is relative to repo root; rootUri is file:///repos/repoName
            var normalized = filePath.replace(/\\/g, '/');
            if (normalized.startsWith('/')) normalized = normalized.substring(1);
            return rootUri.replace(/\/$/, '') + '/' + normalized;
        },

        _sendRequest: function (language, method, params) {
            var self = this;
            var conn = this._connections[language];
            if (!conn) return Promise.reject(new Error('Not connected: ' + language));

            var id = ++this._requestId;
            var msg = { jsonrpc: '2.0', id: id, method: method };
            if (params !== undefined && params !== null) msg.params = params;

            return new Promise(function (resolve, reject) {
                self._pendingRequests[id] = { resolve: resolve, reject: reject, method: method };
                try {
                    conn.ws.send(JSON.stringify(msg));
                } catch (e) {
                    delete self._pendingRequests[id];
                    reject(e);
                }

                // Timeout after 15 seconds
                setTimeout(function () {
                    if (self._pendingRequests[id]) {
                        delete self._pendingRequests[id];
                        reject(new Error('LSP request timeout: ' + method));
                    }
                }, 15000);
            });
        },

        _sendNotification: function (language, method, params) {
            var conn = this._connections[language];
            if (!conn) return;

            var msg = { jsonrpc: '2.0', method: method };
            if (params !== undefined && params !== null) msg.params = params;

            try {
                conn.ws.send(JSON.stringify(msg));
            } catch (e) {
                console.error('[LSP] Failed to send notification:', method, e);
            }
        },

        _handleMessage: function (language, msg) {
            // Response to a request
            if (msg.id !== undefined && msg.id !== null && this._pendingRequests[msg.id]) {
                var pending = this._pendingRequests[msg.id];
                delete this._pendingRequests[msg.id];
                if (msg.error) {
                    pending.reject(msg.error);
                } else {
                    pending.resolve(msg.result);
                }
                return;
            }

            // Server notification
            if (msg.method) {
                this._handleNotification(language, msg.method, msg.params || {});
            }
        },

        _handleNotification: function (language, method, params) {
            if (method === 'textDocument/publishDiagnostics') {
                this._handleDiagnostics(params);
            } else if (method === 'window/logMessage' || method === 'window/showMessage') {
                console.log('[LSP ' + language + '] ' + (params.message || ''));
            }
        },

        _handleDiagnostics: function (params) {
            var uri = params.uri || '';
            var diagnostics = params.diagnostics || [];

            // Find the Monaco model for this URI
            var model = null;
            for (var filePath in _models) {
                var m = _models[filePath];
                if (m && !m.isDisposed() && m.uri.toString() === uri) {
                    model = m;
                    break;
                }
            }

            // Also try matching by path suffix
            if (!model) {
                var uriPath = uri.replace(/^file:\/\//, '');
                for (var filePath in _models) {
                    if (uri.endsWith('/' + filePath.replace(/\\/g, '/'))) {
                        model = _models[filePath];
                        break;
                    }
                }
            }

            if (!model || model.isDisposed()) return;

            // Map LSP diagnostics to Monaco markers
            var severityMap = { 1: 8, 2: 4, 3: 2, 4: 1 }; // LSP Error=1->Monaco 8, Warning=2->4, Info=3->2, Hint=4->1
            var markers = diagnostics.map(function (d) {
                var range = d.range || { start: { line: 0, character: 0 }, end: { line: 0, character: 0 } };
                return {
                    startLineNumber: (range.start.line || 0) + 1,
                    startColumn: (range.start.character || 0) + 1,
                    endLineNumber: (range.end.line || 0) + 1,
                    endColumn: (range.end.character || 0) + 1,
                    message: d.message || '',
                    severity: severityMap[d.severity] || 8,
                    source: d.source || 'lsp',
                    code: d.code
                };
            });

            monaco.editor.setModelMarkers(model, 'lsp', markers);

            // Notify Blazor that diagnostics changed
            if (this._dotNetRef) {
                try { this._dotNetRef.invokeMethodAsync('OnDiagnosticsChanged'); } catch (e) { }
            }
        },

        /**
         * Register all Monaco language providers backed by LSP.
         */
        _registerProviders: function (language) {
            var self = this;
            var caps = this._serverCapabilities[language] || {};

            // Determine which Monaco language IDs this server handles
            var monacoLangs = [];
            for (var mLang in this._languageMap) {
                if (this._languageMap[mLang] === language) {
                    monacoLangs.push(mLang);
                }
            }
            if (monacoLangs.length === 0) monacoLangs.push(language);

            monacoLangs.forEach(function (langId) {
                // Completion provider
                if (caps.completionProvider) {
                    var triggerChars = (caps.completionProvider.triggerCharacters || ['.', '(', '"', "'"]);
                    self._disposables.push(monaco.languages.registerCompletionItemProvider(langId, {
                        triggerCharacters: triggerChars,
                        provideCompletionItems: function (model, position, context) {
                            var lspLang = self._languageMap[langId] || langId;
                            var conn = self._connections[lspLang];
                            if (!conn || !conn.ready) return { suggestions: [] };

                            var uri = model.uri.toString();
                            return self._sendRequest(lspLang, 'textDocument/completion', {
                                textDocument: { uri: uri },
                                position: { line: position.lineNumber - 1, character: position.column - 1 },
                                context: { triggerKind: context.triggerKind === 1 ? 1 : 2 }
                            }).then(function (result) {
                                var items = Array.isArray(result) ? result : (result && result.items ? result.items : []);
                                return {
                                    suggestions: items.map(function (item) {
                                        return self._mapCompletionItem(item, position);
                                    })
                                };
                            }).catch(function () {
                                return { suggestions: [] };
                            });
                        }
                    }));
                }

                // Hover provider
                if (caps.hoverProvider) {
                    self._disposables.push(monaco.languages.registerHoverProvider(langId, {
                        provideHover: function (model, position) {
                            var lspLang = self._languageMap[langId] || langId;
                            return self._sendRequest(lspLang, 'textDocument/hover', {
                                textDocument: { uri: model.uri.toString() },
                                position: { line: position.lineNumber - 1, character: position.column - 1 }
                            }).then(function (result) {
                                if (!result || !result.contents) return null;
                                var contents = [];
                                if (typeof result.contents === 'string') {
                                    contents.push({ value: result.contents });
                                } else if (result.contents.kind) {
                                    contents.push({ value: result.contents.value || '' });
                                } else if (Array.isArray(result.contents)) {
                                    result.contents.forEach(function (c) {
                                        contents.push({ value: typeof c === 'string' ? c : (c.value || '') });
                                    });
                                } else if (result.contents.value) {
                                    contents.push({ value: result.contents.value });
                                }
                                return { contents: contents };
                            }).catch(function () { return null; });
                        }
                    }));
                }

                // Definition provider
                if (caps.definitionProvider) {
                    self._disposables.push(monaco.languages.registerDefinitionProvider(langId, {
                        provideDefinition: function (model, position) {
                            var lspLang = self._languageMap[langId] || langId;
                            return self._sendRequest(lspLang, 'textDocument/definition', {
                                textDocument: { uri: model.uri.toString() },
                                position: { line: position.lineNumber - 1, character: position.column - 1 }
                            }).then(function (result) {
                                return self._mapLocations(result);
                            }).catch(function () { return null; });
                        }
                    }));
                }

                // References provider
                if (caps.referencesProvider) {
                    self._disposables.push(monaco.languages.registerReferenceProvider(langId, {
                        provideReferences: function (model, position, context) {
                            var lspLang = self._languageMap[langId] || langId;
                            return self._sendRequest(lspLang, 'textDocument/references', {
                                textDocument: { uri: model.uri.toString() },
                                position: { line: position.lineNumber - 1, character: position.column - 1 },
                                context: { includeDeclaration: true }
                            }).then(function (result) {
                                return self._mapLocations(result);
                            }).catch(function () { return []; });
                        }
                    }));
                }

                // Signature help provider
                if (caps.signatureHelpProvider) {
                    var sigTriggers = (caps.signatureHelpProvider.triggerCharacters || ['(', ',']);
                    self._disposables.push(monaco.languages.registerSignatureHelpProvider(langId, {
                        signatureHelpTriggerCharacters: sigTriggers,
                        provideSignatureHelp: function (model, position) {
                            var lspLang = self._languageMap[langId] || langId;
                            return self._sendRequest(lspLang, 'textDocument/signatureHelp', {
                                textDocument: { uri: model.uri.toString() },
                                position: { line: position.lineNumber - 1, character: position.column - 1 }
                            }).then(function (result) {
                                if (!result) return null;
                                return {
                                    value: {
                                        signatures: (result.signatures || []).map(function (sig) {
                                            return {
                                                label: sig.label || '',
                                                documentation: sig.documentation ? { value: typeof sig.documentation === 'string' ? sig.documentation : sig.documentation.value || '' } : undefined,
                                                parameters: (sig.parameters || []).map(function (p) {
                                                    return {
                                                        label: p.label || '',
                                                        documentation: p.documentation ? { value: typeof p.documentation === 'string' ? p.documentation : p.documentation.value || '' } : undefined
                                                    };
                                                })
                                            };
                                        }),
                                        activeSignature: result.activeSignature || 0,
                                        activeParameter: result.activeParameter || 0
                                    },
                                    dispose: function () { }
                                };
                            }).catch(function () { return null; });
                        }
                    }));
                }

                // Document formatting provider
                if (caps.documentFormattingProvider) {
                    self._disposables.push(monaco.languages.registerDocumentFormattingEditProvider(langId, {
                        provideDocumentFormattingEdits: function (model, options) {
                            var lspLang = self._languageMap[langId] || langId;
                            return self._sendRequest(lspLang, 'textDocument/formatting', {
                                textDocument: { uri: model.uri.toString() },
                                options: { tabSize: options.tabSize, insertSpaces: options.insertSpaces }
                            }).then(function (result) {
                                return self._mapTextEdits(result);
                            }).catch(function () { return []; });
                        }
                    }));
                }

                // Rename provider
                if (caps.renameProvider) {
                    self._disposables.push(monaco.languages.registerRenameProvider(langId, {
                        provideRenameEdits: function (model, position, newName) {
                            var lspLang = self._languageMap[langId] || langId;
                            return self._sendRequest(lspLang, 'textDocument/rename', {
                                textDocument: { uri: model.uri.toString() },
                                position: { line: position.lineNumber - 1, character: position.column - 1 },
                                newName: newName
                            }).then(function (result) {
                                return self._mapWorkspaceEdit(result);
                            }).catch(function () { return { edits: [] }; });
                        }
                    }));
                }

                // Code action provider
                if (caps.codeActionProvider) {
                    self._disposables.push(monaco.languages.registerCodeActionProvider(langId, {
                        provideCodeActions: function (model, range, context) {
                            var lspLang = self._languageMap[langId] || langId;
                            var diagnostics = (context.markers || []).map(function (m) {
                                return {
                                    range: {
                                        start: { line: m.startLineNumber - 1, character: m.startColumn - 1 },
                                        end: { line: m.endLineNumber - 1, character: m.endColumn - 1 }
                                    },
                                    message: m.message,
                                    severity: m.severity === 8 ? 1 : m.severity === 4 ? 2 : m.severity === 2 ? 3 : 4
                                };
                            });

                            return self._sendRequest(lspLang, 'textDocument/codeAction', {
                                textDocument: { uri: model.uri.toString() },
                                range: {
                                    start: { line: range.startLineNumber - 1, character: range.startColumn - 1 },
                                    end: { line: range.endLineNumber - 1, character: range.endColumn - 1 }
                                },
                                context: { diagnostics: diagnostics }
                            }).then(function (result) {
                                if (!result) return { actions: [], dispose: function () { } };
                                var actions = result.map(function (action) {
                                    var edits = [];
                                    if (action.edit) {
                                        var we = self._mapWorkspaceEdit(action.edit);
                                        edits = we.edits || [];
                                    }
                                    return {
                                        title: action.title || 'Code Action',
                                        kind: action.kind || 'quickfix',
                                        edit: edits.length > 0 ? { edits: edits } : undefined,
                                        diagnostics: diagnostics
                                    };
                                });
                                return { actions: actions, dispose: function () { } };
                            }).catch(function () {
                                return { actions: [], dispose: function () { } };
                            });
                        }
                    }));
                }

                // Document symbol provider
                if (caps.documentSymbolProvider) {
                    self._disposables.push(monaco.languages.registerDocumentSymbolProvider(langId, {
                        provideDocumentSymbols: function (model) {
                            var lspLang = self._languageMap[langId] || langId;
                            return self._sendRequest(lspLang, 'textDocument/documentSymbol', {
                                textDocument: { uri: model.uri.toString() }
                            }).then(function (result) {
                                if (!result) return [];
                                return self._mapDocumentSymbols(result);
                            }).catch(function () { return []; });
                        }
                    }));
                }
            });
        },

        // ---- Mapping helpers ----

        _mapCompletionItem: function (item, position) {
            // LSP CompletionItemKind -> Monaco CompletionItemKind
            var kindMap = {
                1: 18,  // Text -> Value
                2: 1,   // Method -> Method
                3: 0,   // Function -> Function
                4: 8,   // Constructor -> Constructor
                5: 4,   // Field -> Field
                6: 5,   // Variable -> Variable
                7: 7,   // Class -> Class
                8: 7,   // Interface -> Interface
                9: 8,   // Module -> Module
                10: 9,  // Property -> Property
                11: 24, // Unit -> Unit
                12: 12, // Value -> Value
                13: 15, // Enum -> Enum
                14: 13, // Keyword -> Keyword
                15: 14, // Snippet -> Snippet
                16: 15, // Color -> Color
                17: 16, // File -> File
                18: 17, // Reference -> Reference
                19: 18, // Folder -> Folder
                20: 15, // EnumMember -> Enum
                21: 14, // Constant -> Constant
                22: 7,  // Struct -> Struct
                23: 22, // Event -> Event
                24: 23, // Operator -> Operator
                25: 24  // TypeParameter -> TypeParameter
            };

            var insertText = item.insertText || item.label || '';
            var insertTextRules = 0;

            if (item.insertTextFormat === 2) {
                // Snippet format
                insertTextRules = 4; // Monaco CompletionItemInsertTextRule.InsertAsSnippet
            }

            return {
                label: item.label || '',
                kind: kindMap[item.kind] || 18,
                detail: item.detail || '',
                documentation: item.documentation ? (typeof item.documentation === 'string' ? item.documentation : { value: item.documentation.value || '' }) : undefined,
                insertText: insertText,
                insertTextRules: insertTextRules,
                sortText: item.sortText || item.label || '',
                filterText: item.filterText || item.label || '',
                range: undefined // Let Monaco calculate
            };
        },

        _mapLocations: function (result) {
            if (!result) return [];
            var locations = Array.isArray(result) ? result : [result];
            return locations.map(function (loc) {
                var range = loc.range || { start: { line: 0, character: 0 }, end: { line: 0, character: 0 } };
                return {
                    uri: monaco.Uri.parse(loc.uri || ''),
                    range: new monaco.Range(
                        (range.start.line || 0) + 1,
                        (range.start.character || 0) + 1,
                        (range.end.line || 0) + 1,
                        (range.end.character || 0) + 1
                    )
                };
            });
        },

        _mapTextEdits: function (edits) {
            if (!edits) return [];
            return edits.map(function (edit) {
                var range = edit.range || { start: { line: 0, character: 0 }, end: { line: 0, character: 0 } };
                return {
                    range: new monaco.Range(
                        (range.start.line || 0) + 1,
                        (range.start.character || 0) + 1,
                        (range.end.line || 0) + 1,
                        (range.end.character || 0) + 1
                    ),
                    text: edit.newText || ''
                };
            });
        },

        _mapWorkspaceEdit: function (workspaceEdit) {
            var self = this;
            if (!workspaceEdit) return { edits: [] };

            var edits = [];

            // Handle documentChanges
            if (workspaceEdit.documentChanges) {
                workspaceEdit.documentChanges.forEach(function (docChange) {
                    if (docChange.edits) {
                        edits.push({
                            resource: monaco.Uri.parse(docChange.textDocument.uri),
                            textEdit: self._mapTextEdits(docChange.edits) // mapped as versionId-less
                        });
                    }
                });
            }

            // Handle changes (older format)
            if (workspaceEdit.changes) {
                for (var uri in workspaceEdit.changes) {
                    edits.push({
                        resource: monaco.Uri.parse(uri),
                        textEdit: self._mapTextEdits(workspaceEdit.changes[uri])
                    });
                }
            }

            return { edits: edits };
        },

        _mapDocumentSymbols: function (symbols) {
            var self = this;
            // LSP SymbolKind -> Monaco SymbolKind
            var kindMap = {
                1: 0, 2: 11, 3: 4, 4: 22, 5: 7, 6: 5, 7: 10, 8: 9,
                9: 8, 10: 3, 11: 17, 12: 0, 13: 14, 14: 13, 15: 18,
                16: 19, 17: 1, 18: 20, 19: 21, 20: 2, 21: 12, 22: 11,
                23: 6, 24: 23, 25: 24, 26: 25
            };

            return symbols.map(function (sym) {
                var range = sym.range || sym.location?.range || { start: { line: 0, character: 0 }, end: { line: 0, character: 0 } };
                var selRange = sym.selectionRange || range;

                var result = {
                    name: sym.name || '',
                    detail: sym.detail || '',
                    kind: kindMap[sym.kind] || 0,
                    range: new monaco.Range(
                        (range.start.line || 0) + 1,
                        (range.start.character || 0) + 1,
                        (range.end.line || 0) + 1,
                        (range.end.character || 0) + 1
                    ),
                    selectionRange: new monaco.Range(
                        (selRange.start.line || 0) + 1,
                        (selRange.start.character || 0) + 1,
                        (selRange.end.line || 0) + 1,
                        (selRange.end.character || 0) + 1
                    )
                };

                if (sym.children && sym.children.length > 0) {
                    result.children = self._mapDocumentSymbols(sym.children);
                }

                return result;
            });
        }
    };
})();
