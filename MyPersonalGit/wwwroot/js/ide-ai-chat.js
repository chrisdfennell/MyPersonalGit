/**
 * AI Chat and context menu interop for the Web IDE.
 * Adds AI-powered actions (Explain, Refactor, Generate Tests, Fix, Docs)
 * to the Monaco editor context menu and handles chat panel interactions.
 */
window.ideAiChat = (function () {
    let dotNetRef = null;
    let editorInstance = null;
    let contextMenuDisposables = [];

    function init(dotNetReference) {
        dotNetRef = dotNetReference;
    }

    function scrollToBottom() {
        const container = document.querySelector('.ide-ai-messages');
        if (container) {
            container.scrollTop = container.scrollHeight;
        }
    }

    /**
     * Register AI context menu actions by editor ID (looks up from monacoInterop).
     */
    function registerContextMenuActionsById(editorId) {
        var editor = window.monacoInterop && window.monacoInterop._editors
            ? window.monacoInterop._editors[editorId]
            : null;
        if (editor) registerContextMenuActions(editor);
    }

    /**
     * Register AI context menu actions on a Monaco editor instance.
     */
    function registerContextMenuActions(editor) {
        editorInstance = editor;

        // Dispose any previous registrations
        contextMenuDisposables.forEach(d => d.dispose());
        contextMenuDisposables = [];

        // Group: AI Assistant
        const groupId = 'ai-assistant';

        // 1. Explain Code
        contextMenuDisposables.push(editor.addAction({
            id: 'ai.explainCode',
            label: '✨ AI: Explain Code',
            contextMenuGroupId: groupId,
            contextMenuOrder: 1,
            precondition: 'editorHasSelection',
            run: function (ed) {
                const selection = ed.getSelection();
                const selectedText = ed.getModel().getValueInRange(selection);
                if (selectedText && dotNetRef) {
                    dotNetRef.invokeMethodAsync('OnAiContextAction', 'explain', selectedText);
                }
            }
        }));

        // 2. Refactor Code
        contextMenuDisposables.push(editor.addAction({
            id: 'ai.refactorCode',
            label: '✨ AI: Refactor Code',
            contextMenuGroupId: groupId,
            contextMenuOrder: 2,
            precondition: 'editorHasSelection',
            run: function (ed) {
                const selection = ed.getSelection();
                const selectedText = ed.getModel().getValueInRange(selection);
                if (selectedText && dotNetRef) {
                    dotNetRef.invokeMethodAsync('OnAiContextAction', 'refactor', selectedText);
                }
            }
        }));

        // 3. Generate Tests
        contextMenuDisposables.push(editor.addAction({
            id: 'ai.generateTests',
            label: '✨ AI: Generate Tests',
            contextMenuGroupId: groupId,
            contextMenuOrder: 3,
            precondition: 'editorHasSelection',
            run: function (ed) {
                const selection = ed.getSelection();
                const selectedText = ed.getModel().getValueInRange(selection);
                if (selectedText && dotNetRef) {
                    dotNetRef.invokeMethodAsync('OnAiContextAction', 'tests', selectedText);
                }
            }
        }));

        // 4. Fix Code (uses diagnostics if available)
        contextMenuDisposables.push(editor.addAction({
            id: 'ai.fixCode',
            label: '✨ AI: Fix Code',
            contextMenuGroupId: groupId,
            contextMenuOrder: 4,
            precondition: 'editorHasSelection',
            run: function (ed) {
                const selection = ed.getSelection();
                const selectedText = ed.getModel().getValueInRange(selection);
                // Collect diagnostics in the selection range
                const model = ed.getModel();
                const markers = monaco.editor.getModelMarkers({ resource: model.uri });
                const relevantMarkers = markers.filter(m =>
                    m.startLineNumber >= selection.startLineNumber &&
                    m.endLineNumber <= selection.endLineNumber
                );
                const diagnosticsText = relevantMarkers.map(m =>
                    `Line ${m.startLineNumber}: [${m.severity === 8 ? 'Error' : 'Warning'}] ${m.message}`
                ).join('\n');

                if (selectedText && dotNetRef) {
                    dotNetRef.invokeMethodAsync('OnAiContextAction', 'fix', selectedText, diagnosticsText || 'No specific diagnostics');
                }
            }
        }));

        // 5. Add Documentation
        contextMenuDisposables.push(editor.addAction({
            id: 'ai.addDocs',
            label: '✨ AI: Add Documentation',
            contextMenuGroupId: groupId,
            contextMenuOrder: 5,
            precondition: 'editorHasSelection',
            run: function (ed) {
                const selection = ed.getSelection();
                const selectedText = ed.getModel().getValueInRange(selection);
                if (selectedText && dotNetRef) {
                    dotNetRef.invokeMethodAsync('OnAiContextAction', 'docs', selectedText);
                }
            }
        }));

        // 6. Inline Explain (shows hover tooltip)
        contextMenuDisposables.push(editor.addAction({
            id: 'ai.inlineExplain',
            label: '✨ AI: Inline Explain',
            contextMenuGroupId: groupId,
            contextMenuOrder: 6,
            precondition: 'editorHasSelection',
            keybindings: [monaco.KeyMod.CtrlCmd | monaco.KeyMod.Shift | monaco.KeyCode.KeyE],
            run: function (ed) {
                const selection = ed.getSelection();
                const selectedText = ed.getModel().getValueInRange(selection);
                if (selectedText && dotNetRef) {
                    dotNetRef.invokeMethodAsync('OnAiInlineExplain', selectedText, selection.endLineNumber, selection.endColumn);
                }
            }
        }));
    }

    /**
     * Show an inline explanation widget at a specific position in the editor.
     */
    function showInlineExplanation(editorId, line, column, explanation) {
        const editor = window.monacoInterop && window.monacoInterop._editors
            ? window.monacoInterop._editors[editorId]
            : null;
        if (!editor) return;

        // Remove previous widget
        removeInlineExplanation(editorId);

        const domNode = document.createElement('div');
        domNode.className = 'ide-ai-inline-explain';
        domNode.style.cssText = `
            background: #1e1e2e;
            border: 1px solid #c084fc;
            border-radius: 6px;
            padding: 10px 14px;
            max-width: 500px;
            max-height: 300px;
            overflow-y: auto;
            font-size: 12px;
            line-height: 1.5;
            color: #d4d4d4;
            box-shadow: 0 4px 12px rgba(0,0,0,0.4);
            z-index: 1000;
        `;

        // Simple markdown rendering
        let html = explanation
            .replace(/</g, '&lt;').replace(/>/g, '&gt;')
            .replace(/```(\w*)\n([\s\S]*?)```/g, '<pre style="background:#0d1117;padding:6px;border-radius:4px;margin:4px 0;font-size:11px;overflow-x:auto;"><code>$2</code></pre>')
            .replace(/`([^`]+)`/g, '<code style="background:#2d2d2d;padding:1px 3px;border-radius:2px;">$1</code>')
            .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
            .replace(/\n/g, '<br/>');

        domNode.innerHTML = `
            <div style="display:flex;align-items:center;margin-bottom:6px;gap:4px;">
                <span style="color:#c084fc;">✨</span>
                <strong style="font-size:11px;">AI Explanation</strong>
                <button onclick="ideAiChat.removeInlineExplanation('${editorId}')"
                        style="margin-left:auto;background:none;border:none;color:#888;cursor:pointer;font-size:14px;padding:0;">×</button>
            </div>
            <div>${html}</div>
        `;

        const widget = {
            getId: () => 'ai-inline-explain',
            getDomNode: () => domNode,
            getPosition: () => ({
                position: { lineNumber: line, column: column },
                preference: [monaco.editor.ContentWidgetPositionPreference.BELOW]
            })
        };

        editor._aiExplainWidget = widget;
        editor.addContentWidget(widget);
    }

    function removeInlineExplanation(editorId) {
        const editor = window.monacoInterop && window.monacoInterop._editors
            ? window.monacoInterop._editors[editorId]
            : null;
        if (!editor || !editor._aiExplainWidget) return;
        editor.removeContentWidget(editor._aiExplainWidget);
        editor._aiExplainWidget = null;
    }

    /**
     * Get the currently selected text from the editor.
     */
    function getSelectedText(editorId) {
        const editor = window.monacoInterop && window.monacoInterop._editors
            ? window.monacoInterop._editors[editorId]
            : null;
        if (!editor) return null;
        const selection = editor.getSelection();
        if (!selection || selection.isEmpty()) return null;
        return editor.getModel().getValueInRange(selection);
    }

    /**
     * Replace the current selection with new text (for applying AI refactored code).
     */
    function replaceSelection(editorId, newText) {
        const editor = window.monacoInterop && window.monacoInterop._editors
            ? window.monacoInterop._editors[editorId]
            : null;
        if (!editor) return;
        const selection = editor.getSelection();
        if (!selection) return;

        editor.executeEdits('ai-refactor', [{
            range: selection,
            text: newText
        }]);
    }

    function dispose() {
        contextMenuDisposables.forEach(d => d.dispose());
        contextMenuDisposables = [];
        dotNetRef = null;
        editorInstance = null;
    }

    return {
        init,
        scrollToBottom,
        registerContextMenuActions,
        registerContextMenuActionsById,
        showInlineExplanation,
        removeInlineExplanation,
        getSelectedText,
        replaceSelection,
        dispose
    };
})();
