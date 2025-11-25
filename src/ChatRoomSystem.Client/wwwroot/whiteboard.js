// Whiteboard JavaScript Interop
window.whiteboard = {
    canvases: {},
    contexts: {},
    dotNetRefs: {},
    currentTool: {},
    isDrawing: {},
    lastX: {},
    lastY: {},

    // Initialize whiteboard canvas
    initialize: function (roomId, dotNetRef) {
        const canvas = document.getElementById(`whiteboard-canvas-${roomId}`);
        if (!canvas) {
            console.error('Canvas not found');
            return;
        }

        this.canvases[roomId] = canvas;
        this.contexts[roomId] = canvas.getContext('2d');
        this.dotNetRefs[roomId] = dotNetRef;
        this.currentTool[roomId] = 'pen';
        this.isDrawing[roomId] = false;

        // Setup event listeners
        canvas.addEventListener('mousedown', (e) => this.startDrawing(roomId, e));
        canvas.addEventListener('mousemove', (e) => this.draw(roomId, e));
        canvas.addEventListener('mouseup', (e) => this.stopDrawing(roomId, e));
        canvas.addEventListener('mouseout', (e) => this.stopDrawing(roomId, e));

        // Touch support
        canvas.addEventListener('touchstart', (e) => {
            e.preventDefault();
            const touch = e.touches[0];
            const mouseEvent = new MouseEvent('mousedown', {
                clientX: touch.clientX,
                clientY: touch.clientY
            });
            canvas.dispatchEvent(mouseEvent);
        });

        canvas.addEventListener('touchmove', (e) => {
            e.preventDefault();
            const touch = e.touches[0];
            const mouseEvent = new MouseEvent('mousemove', {
                clientX: touch.clientX,
                clientY: touch.clientY
            });
            canvas.dispatchEvent(mouseEvent);
        });

        canvas.addEventListener('touchend', (e) => {
            e.preventDefault();
            const mouseEvent = new MouseEvent('mouseup', {});
            canvas.dispatchEvent(mouseEvent);
        });

        console.log('Whiteboard initialized:', roomId);
    },

    // Set current drawing tool
    setTool: function (roomId, tool) {
        this.currentTool[roomId] = tool;
    },

    // Get canvas coordinates
    getCoordinates: function (roomId, event) {
        const canvas = this.canvases[roomId];
        const rect = canvas.getBoundingClientRect();
        const scaleX = canvas.width / rect.width;
        const scaleY = canvas.height / rect.height;

        return {
            x: (event.clientX - rect.left) * scaleX,
            y: (event.clientY - rect.top) * scaleY
        };
    },

    // Start drawing
    startDrawing: function (roomId, event) {
        const coords = this.getCoordinates(roomId, event);
        this.isDrawing[roomId] = true;
        this.lastX[roomId] = coords.x;
        this.lastY[roomId] = coords.y;
    },

    // Draw on canvas
    draw: function (roomId, event) {
        if (!this.isDrawing[roomId]) return;

        const coords = this.getCoordinates(roomId, event);
        const ctx = this.contexts[roomId];
        const tool = this.currentTool[roomId];

        // Default drawing style
        ctx.lineCap = 'round';
        ctx.lineJoin = 'round';

        if (tool === 'pen') {
            this.drawLine(roomId, this.lastX[roomId], this.lastY[roomId], coords.x, coords.y);
        } else if (tool === 'eraser') {
            this.erase(roomId, coords.x, coords.y);
        }

        // Notify Blazor (for broadcasting)
        this.dotNetRefs[roomId].invokeMethodAsync('OnDraw', 'draw',
            this.lastX[roomId], this.lastY[roomId], coords.x, coords.y);

        this.lastX[roomId] = coords.x;
        this.lastY[roomId] = coords.y;
    },

    // Stop drawing
    stopDrawing: function (roomId, event) {
        if (!this.isDrawing[roomId]) return;

        const coords = this.getCoordinates(roomId, event);
        const tool = this.currentTool[roomId];

        // Draw shapes on mouse up
        if (tool === 'line') {
            this.drawLine(roomId, this.lastX[roomId], this.lastY[roomId], coords.x, coords.y);
        } else if (tool === 'rect') {
            this.drawRectangle(roomId, this.lastX[roomId], this.lastY[roomId], coords.x, coords.y);
        } else if (tool === 'circle') {
            this.drawCircle(roomId, this.lastX[roomId], this.lastY[roomId], coords.x, coords.y);
        }

        this.isDrawing[roomId] = false;
    },

    // Drawing functions
    drawLine: function (roomId, x1, y1, x2, y2) {
        const ctx = this.contexts[roomId];
        ctx.beginPath();
        ctx.moveTo(x1, y1);
        ctx.lineTo(x2, y2);
        ctx.stroke();
    },

    drawRectangle: function (roomId, x1, y1, x2, y2) {
        const ctx = this.contexts[roomId];
        ctx.strokeRect(x1, y1, x2 - x1, y2 - y1);
    },

    drawCircle: function (roomId, x1, y1, x2, y2) {
        const ctx = this.contexts[roomId];
        const radius = Math.sqrt(Math.pow(x2 - x1, 2) + Math.pow(y2 - y1, 2));
        ctx.beginPath();
        ctx.arc(x1, y1, radius, 0, 2 * Math.PI);
        ctx.stroke();
    },

    erase: function (roomId, x, y) {
        const ctx = this.contexts[roomId];
        ctx.clearRect(x - 10, y - 10, 20, 20);
    },

    // Draw stroke from remote user
    drawRemoteStroke: function (roomId, stroke) {
        const ctx = this.contexts[roomId];
        ctx.strokeStyle = stroke.color;
        ctx.lineWidth = stroke.width;

        if (stroke.points.length < 2) return;

        ctx.beginPath();
        ctx.moveTo(stroke.points[0].x, stroke.points[0].y);

        for (let i = 1; i < stroke.points.length; i++) {
            ctx.lineTo(stroke.points[i].x, stroke.points[i].y);
        }

        ctx.stroke();
    },

    // Clear canvas
    clear: function (roomId) {
        const canvas = this.canvases[roomId];
        const ctx = this.contexts[roomId];
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        console.log('Canvas cleared:', roomId);
    },

    // Export canvas as image
    export: function (roomId) {
        const canvas = this.canvases[roomId];
        return canvas.toDataURL('image/png');
    },

    // Download canvas as image
    download: function (dataUrl, filename) {
        const link = document.createElement('a');
        link.href = dataUrl;
        link.download = filename;
        link.click();
    },

    // Dispose
    dispose: function (roomId) {
        delete this.canvases[roomId];
        delete this.contexts[roomId];
        delete this.dotNetRefs[roomId];
        delete this.currentTool[roomId];
        delete this.isDrawing[roomId];
        delete this.lastX[roomId];
        delete this.lastY[roomId];
        console.log('Whiteboard disposed:', roomId);
    }
};
