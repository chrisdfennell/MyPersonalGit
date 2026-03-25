/**
 * Real-time collaboration interop for the Web IDE.
 * Handles SignalR connection, cursor/selection sync, remote edit application,
 * and collaborative presence indicators via Monaco editor decorations.
 */
window.ideCollaboration = (function () {
    let connection = null;
    let dotNetRef = null;
    let editorInstance = null;
    let sessionKey = null;
    let localUserId = null;
    let suppressNextEdit = false;

    // Remote participant decorations
    const participants = new Map(); // connectionId -> { username, color, cursorDeco, selectionDeco, labelWidget }

    // Pastel colors for participant cursors
    const COLORS = [
        '#ff6b6b', '#feca57', '#48dbfb', '#ff9ff3', '#54a0ff',
        '#5f27cd', '#01a3a4', '#f368e0', '#ee5a24', '#2ed573'
    ];

    function getColorForIndex(idx) {
        return COLORS[idx % COLORS.length];
    }

    async function init(dotNetReference, hubUrl) {
        dotNetRef = dotNetReference;

        if (connection) {
            try { await connection.stop(); } catch { }
        }

        connection = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl || '/hubs/collaboration')
            .withAutomaticReconnect([0, 1000, 2000, 5000, 10000])
            .build();

        connection.on('UserJoined', (participant) => {
            addRemoteParticipant(participant);
            if (dotNetRef) dotNetRef.invokeMethodAsync('OnCollabUserJoined', participant.username, participant.color);
        });

        connection.on('UserLeft', (connectionId) => {
            removeRemoteParticipant(connectionId);
            if (dotNetRef) dotNetRef.invokeMethodAsync('OnCollabUserLeft', connectionId);
        });

        connection.on('SessionJoined', (data) => {
            sessionKey = data.sessionKey;
            data.participants.forEach(p => addRemoteParticipant(p));
            if (dotNetRef) dotNetRef.invokeMethodAsync('OnCollabSessionJoined', data.participants.length);
        });

        connection.on('CursorMoved', (data) => {
            updateRemoteCursor(data.connectionId, data.line, data.column);
        });

        connection.on('SelectionChanged', (data) => {
            updateRemoteSelection(data.connectionId, data.startLine, data.startColumn, data.endLine, data.endColumn);
        });

        connection.on('EditApplied', (data) => {
            applyRemoteEdit(data);
        });

        connection.on('SyncRequested', (targetConnectionId) => {
            if (editorInstance) {
                const content = editorInstance.getValue();
                connection.invoke('SendSync', targetConnectionId, content, 0);
            }
        });

        connection.on('SyncReceived', (data) => {
            if (editorInstance) {
                suppressNextEdit = true;
                editorInstance.setValue(data.content);
                suppressNextEdit = false;
            }
        });

        connection.onreconnected(() => {
            if (dotNetRef) dotNetRef.invokeMethodAsync('OnCollabReconnected');
        });

        connection.onclose(() => {
            if (dotNetRef) dotNetRef.invokeMethodAsync('OnCollabDisconnected');
        });

        await connection.start();
        localUserId = connection.connectionId;
    }

    async function joinSession(repoName, filePath, username, color) {
        if (!connection) return;
        sessionKey = `${repoName}:${filePath}`;
        await connection.invoke('JoinSession', repoName, filePath, username, color);
    }

    async function leaveSession(repoName, filePath) {
        if (!connection) return;
        await connection.invoke('LeaveSession', repoName, filePath);
        clearAllDecorations();
        sessionKey = null;
    }

    function attachEditor(editor) {
        editorInstance = editor;
        if (!editor) return;

        // Track cursor changes
        editor.onDidChangeCursorPosition((e) => {
            if (connection && sessionKey) {
                connection.invoke('UpdateCursor', sessionKey, e.position.lineNumber, e.position.column).catch(() => { });
            }
        });

        // Track selection changes
        editor.onDidChangeCursorSelection((e) => {
            if (connection && sessionKey) {
                const sel = e.selection;
                if (!sel.isEmpty()) {
                    connection.invoke('UpdateSelection', sessionKey,
                        sel.startLineNumber, sel.startColumn,
                        sel.endLineNumber, sel.endColumn).catch(() => { });
                }
            }
        });

        // Track content changes and broadcast edits
        editor.onDidChangeModelContent((e) => {
            if (suppressNextEdit || !connection || !sessionKey) return;

            for (const change of e.changes) {
                const op = {
                    startLine: change.range.startLineNumber,
                    startColumn: change.range.startColumn,
                    endLine: change.range.endLineNumber,
                    endColumn: change.range.endColumn,
                    text: change.text
                };
                connection.invoke('ApplyEdit', sessionKey, op).catch(() => { });
            }
        });
    }

    function addRemoteParticipant(participant) {
        if (participant.connectionId === localUserId) return;

        participants.set(participant.connectionId, {
            username: participant.username,
            color: participant.color || getColorForIndex(participants.size),
            cursorDecorations: [],
            selectionDecorations: [],
            cursorLine: participant.cursorLine || 1,
            cursorColumn: participant.cursorColumn || 1
        });

        updateRemoteCursor(participant.connectionId, participant.cursorLine || 1, participant.cursorColumn || 1);
    }

    function removeRemoteParticipant(connectionId) {
        const p = participants.get(connectionId);
        if (!p || !editorInstance) return;

        // Clear decorations
        if (p.cursorDecorations.length > 0) {
            editorInstance.deltaDecorations(p.cursorDecorations, []);
        }
        if (p.selectionDecorations.length > 0) {
            editorInstance.deltaDecorations(p.selectionDecorations, []);
        }

        // Remove label widget
        if (p.labelWidget) {
            editorInstance.removeContentWidget(p.labelWidget);
        }

        participants.delete(connectionId);
    }

    function updateRemoteCursor(connectionId, line, column) {
        const p = participants.get(connectionId);
        if (!p || !editorInstance) return;

        p.cursorLine = line;
        p.cursorColumn = column;

        // Update cursor decoration
        const cursorClassName = `collab-cursor-${connectionId.replace(/[^a-zA-Z0-9]/g, '')}`;

        // Inject dynamic CSS for the cursor color if not already present
        ensureCursorStyle(cursorClassName, p.color);

        const newDecos = [{
            range: new monaco.Range(line, column, line, column + 1),
            options: {
                className: cursorClassName,
                stickiness: monaco.editor.TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges
            }
        }];

        p.cursorDecorations = editorInstance.deltaDecorations(p.cursorDecorations, newDecos);

        // Update or create label widget
        updateCursorLabel(connectionId, p, line, column);
    }

    function updateRemoteSelection(connectionId, startLine, startColumn, endLine, endColumn) {
        const p = participants.get(connectionId);
        if (!p || !editorInstance) return;

        const selClassName = `collab-selection-${connectionId.replace(/[^a-zA-Z0-9]/g, '')}`;
        ensureSelectionStyle(selClassName, p.color);

        const newDecos = [{
            range: new monaco.Range(startLine, startColumn, endLine, endColumn),
            options: {
                className: selClassName,
                stickiness: monaco.editor.TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges
            }
        }];

        p.selectionDecorations = editorInstance.deltaDecorations(p.selectionDecorations, newDecos);

        // Also update cursor to end of selection
        updateRemoteCursor(connectionId, endLine, endColumn);
    }

    function applyRemoteEdit(data) {
        if (!editorInstance || data.connectionId === localUserId) return;

        suppressNextEdit = true;
        const model = editorInstance.getModel();
        if (model) {
            model.pushEditOperations([], [{
                range: new monaco.Range(data.startLine, data.startColumn, data.endLine, data.endColumn),
                text: data.text
            }], () => null);
        }
        suppressNextEdit = false;
    }

    function ensureCursorStyle(className, color) {
        if (document.querySelector(`style[data-collab="${className}"]`)) return;
        const style = document.createElement('style');
        style.setAttribute('data-collab', className);
        style.textContent = `
            .${className} {
                background: ${color};
                width: 2px !important;
                margin-left: -1px;
            }
            .${className}::after {
                content: '';
                position: absolute;
                top: -2px;
                left: -2px;
                width: 6px;
                height: 6px;
                background: ${color};
                border-radius: 50%;
            }
        `;
        document.head.appendChild(style);
    }

    function ensureSelectionStyle(className, color) {
        if (document.querySelector(`style[data-collab="${className}"]`)) return;
        const style = document.createElement('style');
        style.setAttribute('data-collab', className);
        style.textContent = `.${className} { background: ${color}33; }`;
        document.head.appendChild(style);
    }

    function updateCursorLabel(connectionId, participant, line, column) {
        if (!editorInstance) return;

        // Remove existing widget
        if (participant.labelWidget) {
            editorInstance.removeContentWidget(participant.labelWidget);
        }

        const widgetId = `collab-label-${connectionId.replace(/[^a-zA-Z0-9]/g, '')}`;
        const domNode = document.createElement('div');
        domNode.className = 'collab-cursor-label';
        domNode.style.cssText = `
            background: ${participant.color};
            color: #fff;
            font-size: 10px;
            padding: 1px 4px;
            border-radius: 2px;
            white-space: nowrap;
            pointer-events: none;
            z-index: 100;
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
        `;
        domNode.textContent = participant.username;

        const widget = {
            getId: () => widgetId,
            getDomNode: () => domNode,
            getPosition: () => ({
                position: { lineNumber: line, column: column },
                preference: [monaco.editor.ContentWidgetPositionPreference.ABOVE]
            })
        };

        participant.labelWidget = widget;
        editorInstance.addContentWidget(widget);

        // Auto-hide label after 3 seconds
        clearTimeout(participant.labelTimeout);
        participant.labelTimeout = setTimeout(() => {
            domNode.style.opacity = '0';
            domNode.style.transition = 'opacity 0.5s';
        }, 3000);
    }

    function clearAllDecorations() {
        if (!editorInstance) return;
        for (const [id, p] of participants) {
            if (p.cursorDecorations.length > 0) editorInstance.deltaDecorations(p.cursorDecorations, []);
            if (p.selectionDecorations.length > 0) editorInstance.deltaDecorations(p.selectionDecorations, []);
            if (p.labelWidget) editorInstance.removeContentWidget(p.labelWidget);
        }
        participants.clear();
    }

    async function disconnect() {
        if (connection) {
            try { await connection.stop(); } catch { }
            connection = null;
        }
        clearAllDecorations();
        sessionKey = null;
    }

    function getParticipants() {
        const result = [];
        for (const [id, p] of participants) {
            result.push({ connectionId: id, username: p.username, color: p.color });
        }
        return result;
    }

    function isConnected() {
        return connection && connection.state === 'Connected';
    }

    function attachEditorById(editorId) {
        var editor = window.monacoInterop && window.monacoInterop._editors
            ? window.monacoInterop._editors[editorId]
            : null;
        if (editor) attachEditor(editor);
    }

    return {
        init,
        joinSession,
        leaveSession,
        attachEditor,
        attachEditorById,
        disconnect,
        getParticipants,
        isConnected
    };
})();
