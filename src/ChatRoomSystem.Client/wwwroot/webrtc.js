// WebRTC JavaScript Interop
window.webRTC = {
    dotNetRef: null,
    peerConnections: {},
    localStreams: {},
    remoteStreams: {},

    // ICE servers configuration (STUN server cho NAT traversal)
    iceServers: {
        iceServers: [
            { urls: 'stun:stun.l.google.com:19302' },
            { urls: 'stun:stun1.l.google.com:19302' }
        ]
    },

    // Initialize WebRTC
    initialize: function (dotNetReference) {
        this.dotNetRef = dotNetReference;
        console.log('WebRTC initialized');
    },

    // Create peer connection
    createPeerConnection: function (callId) {
        const pc = new RTCPeerConnection(this.iceServers);

        // Handle ICE candidate
        pc.onicecandidate = (event) => {
            if (event.candidate) {
                const candidate = JSON.stringify(event.candidate);
                this.dotNetRef.invokeMethodAsync('OnIceCandidate', callId, candidate);
            }
        };

        // Handle remote stream
        pc.ontrack = (event) => {
            console.log('Remote track received:', event.streams[0].id);
            this.remoteStreams[callId] = event.streams[0];

            // Attach to video element
            const remoteVideo = document.getElementById(`remote-video-${callId}`);
            if (remoteVideo) {
                remoteVideo.srcObject = event.streams[0];
            }

            this.dotNetRef.invokeMethodAsync('OnRemoteStream', callId, event.streams[0].id);
        };

        // Handle connection state
        pc.onconnectionstatechange = () => {
            console.log('Connection state:', pc.connectionState);
        };

        this.peerConnections[callId] = pc;
        console.log('Peer connection created:', callId);
    },

    // Get local media stream (camera + microphone)
    getLocalStream: async function (callId, video, audio) {
        try {
            const constraints = {
                video: video ? { width: 1280, height: 720 } : false,
                audio: audio
            };

            const stream = await navigator.mediaDevices.getUserMedia(constraints);
            this.localStreams[callId] = stream;

            // Attach to local video element
            const localVideo = document.getElementById(`local-video-${callId}`);
            if (localVideo) {
                localVideo.srcObject = stream;
            }

            // Add tracks to peer connection
            const pc = this.peerConnections[callId];
            if (pc) {
                stream.getTracks().forEach(track => {
                    pc.addTrack(track, stream);
                });
            }

            console.log('Local stream obtained:', stream.id);
            return true;
        } catch (error) {
            console.error('Error getting local stream:', error);
            alert('Could not access camera/microphone. Please grant permissions.');
            return false;
        }
    },

    // Create SDP offer
    createOffer: async function (callId) {
        const pc = this.peerConnections[callId];
        if (!pc) {
            throw new Error('Peer connection not found');
        }

        try {
            const offer = await pc.createOffer();
            await pc.setLocalDescription(offer);
            return JSON.stringify(offer);
        } catch (error) {
            console.error('Error creating offer:', error);
            throw error;
        }
    },

    // Create SDP answer
    createAnswer: async function (callId) {
        const pc = this.peerConnections[callId];
        if (!pc) {
            throw new Error('Peer connection not found');
        }

        try {
            const answer = await pc.createAnswer();
            await pc.setLocalDescription(answer);
            return JSON.stringify(answer);
        } catch (error) {
            console.error('Error creating answer:', error);
            throw error;
        }
    },

    // Set remote description (offer or answer)
    setRemoteDescription: async function (callId, sdpJson, type) {
        const pc = this.peerConnections[callId];
        if (!pc) {
            throw new Error('Peer connection not found');
        }

        try {
            const sdp = JSON.parse(sdpJson);
            await pc.setRemoteDescription(new RTCSessionDescription(sdp));
            console.log('Remote description set:', type);
        } catch (error) {
            console.error('Error setting remote description:', error);
            throw error;
        }
    },

    // Add ICE candidate
    addIceCandidate: async function (callId, candidateJson) {
        const pc = this.peerConnections[callId];
        if (!pc) {
            throw new Error('Peer connection not found');
        }

        try {
            const candidate = JSON.parse(candidateJson);
            await pc.addIceCandidate(new RTCIceCandidate(candidate));
            console.log('ICE candidate added');
        } catch (error) {
            console.error('Error adding ICE candidate:', error);
        }
    },

    // Toggle video on/off
    toggleVideo: function (callId) {
        const stream = this.localStreams[callId];
        if (stream) {
            const videoTrack = stream.getVideoTracks()[0];
            if (videoTrack) {
                videoTrack.enabled = !videoTrack.enabled;
                console.log('Video toggled:', videoTrack.enabled);
                return videoTrack.enabled;
            }
        }
        return false;
    },

    // Toggle audio on/off
    toggleAudio: function (callId) {
        const stream = this.localStreams[callId];
        if (stream) {
            const audioTrack = stream.getAudioTracks()[0];
            if (audioTrack) {
                audioTrack.enabled = !audioTrack.enabled;
                console.log('Audio toggled:', audioTrack.enabled);
                return audioTrack.enabled;
            }
        }
        return false;
    },

    // Start screen sharing
    startScreenShare: async function (callId) {
        try {
            const screenStream = await navigator.mediaDevices.getDisplayMedia({
                video: { cursor: 'always' },
                audio: false
            });

            const pc = this.peerConnections[callId];
            if (pc) {
                // Replace video track với screen share track
                const videoTrack = screenStream.getVideoTracks()[0];
                const sender = pc.getSenders().find(s => s.track && s.track.kind === 'video');

                if (sender) {
                    await sender.replaceTrack(videoTrack);
                }

                // Update local video element
                const localVideo = document.getElementById(`local-video-${callId}`);
                if (localVideo) {
                    localVideo.srcObject = screenStream;
                }

                // Handle screen share stopped (user clicks "Stop sharing")
                videoTrack.onended = () => {
                    this.stopScreenShare(callId);
                };

                console.log('Screen sharing started');
            }
        } catch (error) {
            console.error('Error starting screen share:', error);
            throw error;
        }
    },

    // Stop screen sharing
    stopScreenShare: async function (callId) {
        const pc = this.peerConnections[callId];
        const originalStream = this.localStreams[callId];

        if (pc && originalStream) {
            // Replace screen track với original camera track
            const videoTrack = originalStream.getVideoTracks()[0];
            const sender = pc.getSenders().find(s => s.track && s.track.kind === 'video');

            if (sender && videoTrack) {
                await sender.replaceTrack(videoTrack);
            }

            // Restore local video element
            const localVideo = document.getElementById(`local-video-${callId}`);
            if (localVideo) {
                localVideo.srcObject = originalStream;
            }

            console.log('Screen sharing stopped');
        }
    },

    // Close peer connection và cleanup
    closeConnection: function (callId) {
        // Stop local stream
        const localStream = this.localStreams[callId];
        if (localStream) {
            localStream.getTracks().forEach(track => track.stop());
            delete this.localStreams[callId];
        }

        // Stop remote stream
        const remoteStream = this.remoteStreams[callId];
        if (remoteStream) {
            remoteStream.getTracks().forEach(track => track.stop());
            delete this.remoteStreams[callId];
        }

        // Close peer connection
        const pc = this.peerConnections[callId];
        if (pc) {
            pc.close();
            delete this.peerConnections[callId];
        }

        console.log('Connection closed:', callId);
    }
};
