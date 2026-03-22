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
         * Update the stored original content for a file (after auto-save).
         * @param {string} editorId - The editor instance ID.
         * @param {string} filePath - The file path.
         * @param {string} content - The new original content.
         */
        updateOriginalContent: function (editorId, filePath, content) {
            _originalContent[filePath] = content;
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
})();
