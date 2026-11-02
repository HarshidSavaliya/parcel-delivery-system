/**
 * Parcel Delivery System - Interactive Mapping & Live GPS Telemetry Engine
 * Powered by OpenStreetMap (OSM) & Leaflet.js with Live Geolocation,
 * Reverse Geocoding, Draggable Routing Pins, and Real-Time Courier Tracking.
 */
window.ParcelMap = {
    mapInstances: {},
    animTimers: {},

    /**
     * Helper to get user's current GPS device location
     */
    getCurrentLocation: function (onSuccess, onError) {
        if (!navigator.geolocation) {
            if (onError) onError('Geolocation is not supported by your browser.');
            return;
        }

        navigator.geolocation.getCurrentPosition(
            function (pos) {
                if (onSuccess) {
                    onSuccess({
                        lat: pos.coords.latitude,
                        lon: pos.coords.longitude,
                        accuracy: pos.coords.accuracy
                    });
                }
            },
            function (err) {
                let msg = 'Unable to fetch your GPS coordinates.';
                if (err.code === 1) msg = 'Location permission was denied. Please allow location access.';
                else if (err.code === 2) msg = 'Location position is currently unavailable.';
                else if (err.code === 3) msg = 'Location request timed out.';
                if (onError) onError(msg);
            },
            { enableHighAccuracy: true, timeout: 12000, maximumAge: 10000 }
        );
    },

    /**
     * Helper to reverse-geocode coordinates to an address
     */
    reverseGeocode: function (lat, lon, callback) {
        fetch(`/api/location/reverse?lat=${encodeURIComponent(lat)}&lon=${encodeURIComponent(lon)}`)
            .then(res => {
                if (!res.ok) throw new Error('Reverse geocode failed');
                return res.json();
            })
            .then(data => {
                if (callback) callback(data);
            })
            .catch(err => {
                console.warn('[ParcelMap] Reverse geocoding lookup failed:', err);
                if (callback) {
                    callback({
                        formattedAddress: `Pin Location (${lat.toFixed(4)}, ${lon.toFixed(4)})`,
                        street: `Location (${lat.toFixed(4)}, ${lon.toFixed(4)})`,
                        city: '',
                        state: '',
                        postalCode: '',
                        country: '',
                        latitude: lat,
                        longitude: lon
                    });
                }
            });
    },

    /**
     * Creates custom styled badges for map markers
     */
    createIcon: function (type) {
        let bg = '#4f46e5';
        let iconClass = 'bi-box-seam-fill';
        let shadowColor = 'rgba(79, 70, 229, 0.4)';

        if (type === 'delivery' || type === 'dropoff') {
            bg = '#10b981';
            iconClass = 'bi-geo-alt-fill';
            shadowColor = 'rgba(16, 185, 129, 0.4)';
        } else if (type === 'agent' || type === 'truck') {
            bg = '#f59e0b';
            iconClass = 'bi-truck';
            shadowColor = 'rgba(245, 158, 11, 0.5)';
            return L.divIcon({
                className: 'pds-map-custom-marker',
                html: '<div style="position:relative;">' +
                      '<div style="position:absolute;top:-6px;left:-6px;width:46px;height:46px;border-radius:50%;background:rgba(245,158,11,0.25);animation:pds-pulse 1.8s infinite;"></div>' +
                      '<div style="background:' + bg + ';color:#ffffff;width:34px;height:34px;border-radius:50%;display:flex;align-items:center;justify-content:center;box-shadow:0 4px 12px ' + shadowColor + ';border:2px solid #ffffff;font-size:16px;position:relative;z-index:2;"><i class="bi ' + iconClass + '"></i></div>' +
                      '</div>',
                iconSize: [34, 34],
                iconAnchor: [17, 17],
                popupAnchor: [0, -18]
            });
        } else if (type === 'gps' || type === 'current') {
            bg = '#0284c7';
            iconClass = 'bi-crosshair';
            shadowColor = 'rgba(2, 132, 199, 0.4)';
        } else if (type === 'hub' || type === 'warehouse') {
            bg = '#0f172a';
            iconClass = 'bi-building';
            shadowColor = 'rgba(15, 23, 42, 0.4)';
        }

        return L.divIcon({
            className: 'pds-map-custom-marker',
            html: '<div style="background:' + bg + ';color:#ffffff;width:34px;height:34px;border-radius:50%;display:flex;align-items:center;justify-content:center;box-shadow:0 4px 12px ' + shadowColor + ';border:2px solid #ffffff;font-size:15px;"><i class="bi ' + iconClass + '"></i></div>',
            iconSize: [34, 34],
            iconAnchor: [17, 17],
            popupAnchor: [0, -18]
        });
    },

    /**
     * Legacy Alias for initRouteMap
     */
    init: function (containerId, pickupLat, pickupLon, deliveryLat, deliveryLon, geometryJson) {
        return this.initRouteMap(containerId, pickupLat, pickupLon, deliveryLat, deliveryLon, geometryJson);
    },

    /**
     * Initializes standard route map
     */
    initRouteMap: function (containerId, pickupLat, pickupLon, deliveryLat, deliveryLon, geometryJson) {
        const container = document.getElementById(containerId);
        if (!container || typeof L === 'undefined') return null;

        if (this.mapInstances[containerId]) {
            try { this.mapInstances[containerId].remove(); } catch (e) { }
            delete this.mapInstances[containerId];
        }

        const pLat = parseFloat(pickupLat);
        const pLon = parseFloat(pickupLon);
        const dLat = parseFloat(deliveryLat);
        const dLon = parseFloat(deliveryLon);

        const hasPickup = !isNaN(pLat) && !isNaN(pLon) && (pLat !== 0 || pLon !== 0);
        const hasDelivery = !isNaN(dLat) && !isNaN(dLon) && (dLat !== 0 || dLon !== 0);

        const defaultCenter = hasPickup ? [pLat, pLon] : (hasDelivery ? [dLat, dLon] : [28.6139, 77.2090]);
        const defaultZoom = hasPickup && hasDelivery ? 12 : 11;

        const map = L.map(containerId, {
            scrollWheelZoom: true,
            zoomControl: true
        }).setView(defaultCenter, defaultZoom);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener">OpenStreetMap</a>',
            maxZoom: 19
        }).addTo(map);

        const featureItems = [];

        if (hasPickup) {
            const pMarker = L.marker([pLat, pLon], { icon: this.createIcon('pickup') })
                .addTo(map)
                .bindPopup('<div style="font-size:13px;"><strong>📦 Pickup Location</strong><br/><span style="color:#64748b;">' + pLat.toFixed(4) + ', ' + pLon.toFixed(4) + '</span></div>');
            featureItems.push(pMarker);
        }

        if (hasDelivery) {
            const dMarker = L.marker([dLat, dLon], { icon: this.createIcon('delivery') })
                .addTo(map)
                .bindPopup('<div style="font-size:13px;"><strong>📍 Delivery Destination</strong><br/><span style="color:#64748b;">' + dLat.toFixed(4) + ', ' + dLon.toFixed(4) + '</span></div>');
            featureItems.push(dMarker);
        }

        if (hasPickup && hasDelivery) {
            let routeDrawn = false;
            if (geometryJson) {
                try {
                    const geoData = typeof geometryJson === 'string' ? JSON.parse(geometryJson) : geometryJson;
                    const routeLayer = L.geoJSON(geoData, {
                        style: { color: '#4f46e5', weight: 5, opacity: 0.85 }
                    }).addTo(map);
                    featureItems.push(routeLayer);
                    routeDrawn = true;
                } catch (e) { }
            }

            if (!routeDrawn) {
                const line = L.polyline([[pLat, pLon], [dLat, dLon]], {
                    color: '#4f46e5',
                    weight: 4,
                    dashArray: '6, 8',
                    opacity: 0.8
                }).addTo(map);
                featureItems.push(line);
            }

            try {
                if (featureItems.length > 0) {
                    const group = new L.featureGroup(featureItems);
                    map.fitBounds(group.getBounds().pad(0.2));
                }
            } catch (err) { }
        }

        setTimeout(function () { map.invalidateSize(); }, 200);
        this.mapInstances[containerId] = map;
        return map;
    },

    /**
     * Initializes Live Tracking Map with simulated/real-time Courier Vehicle movement
     */
    initLiveTrackingMap: function (containerId, pickupLat, pickupLon, deliveryLat, deliveryLon, geometryJson, status) {
        const container = document.getElementById(containerId);
        if (!container || typeof L === 'undefined') return null;

        if (this.animTimers[containerId]) {
            clearInterval(this.animTimers[containerId]);
            delete this.animTimers[containerId];
        }

        const map = this.initRouteMap(containerId, pickupLat, pickupLon, deliveryLat, deliveryLon, geometryJson);
        if (!map) return null;

        const pLat = parseFloat(pickupLat);
        const pLon = parseFloat(pickupLon);
        const dLat = parseFloat(deliveryLat);
        const dLon = parseFloat(deliveryLon);

        const hasPickup = !isNaN(pLat) && !isNaN(pLon) && (pLat !== 0 || pLon !== 0);
        const hasDelivery = !isNaN(dLat) && !isNaN(dLon) && (dLat !== 0 || dLon !== 0);

        if (!hasPickup || !hasDelivery) return map;

        // Build list of waypoints along route
        let waypoints = [[pLat, pLon], [dLat, dLon]];
        if (geometryJson) {
            try {
                const geoData = typeof geometryJson === 'string' ? JSON.parse(geometryJson) : geometryJson;
                if (geoData.type === 'LineString' && Array.isArray(geoData.coordinates)) {
                    waypoints = geoData.coordinates.map(c => [c[1], c[0]]);
                } else if (geoData.coordinates && Array.isArray(geoData.coordinates)) {
                    waypoints = geoData.coordinates.map(c => [c[1], c[0]]);
                }
            } catch (e) { }
        }

        // Determine if Courier is actively moving
        const isDelivered = status === 'Delivered';
        const isCancelled = status === 'Cancelled';
        const isActive = !isDelivered && !isCancelled;

        let initialIndex = 0;
        if (status === 'OutForDelivery') {
            initialIndex = Math.floor(waypoints.length * 0.75);
        } else if (status === 'InTransit' || status === 'PickedUp') {
            initialIndex = Math.floor(waypoints.length * 0.45);
        } else if (status === 'Delivered') {
            initialIndex = waypoints.length - 1;
        }

        const startCoord = waypoints[initialIndex] || [pLat, pLon];
        const agentMarker = L.marker(startCoord, {
            icon: this.createIcon(isDelivered ? 'delivery' : 'truck'),
            zIndexOffset: 1000
        }).addTo(map);

        const popupText = isDelivered ?
            '<strong>✅ Package Delivered</strong>' :
            '<strong>🚚 Delivery Agent in Transit</strong><br/><span style="color:#059669;font-weight:600;"><i class="bi bi-broadcast me-1"></i>Live Telemetry Active</span>';

        agentMarker.bindPopup(popupText);

        if (isActive && waypoints.length > 2) {
            let currentIndex = initialIndex;
            let direction = 1;

            this.animTimers[containerId] = setInterval(function () {
                currentIndex += direction;
                if (currentIndex >= waypoints.length - 1) {
                    currentIndex = waypoints.length - 1;
                    direction = -1;
                } else if (currentIndex <= 0) {
                    currentIndex = 0;
                    direction = 1;
                }

                const nextPos = waypoints[currentIndex];
                agentMarker.setLatLng(nextPos);
            }, 2500);
        }

        return map;
    },

    /**
     * Initializes Interactive Map for Booking Form (Click to Pin, Drag to update, Live GPS)
     */
    initBookingInteractiveMap: function (containerId, options) {
        const container = document.getElementById(containerId);
        if (!container || typeof L === 'undefined') return null;

        if (this.mapInstances[containerId]) {
            try { this.mapInstances[containerId].remove(); } catch (e) { }
            delete this.mapInstances[containerId];
        }

        const self = this;
        let pLat = parseFloat(options.pickupLat) || 44.0462;
        let pLon = parseFloat(options.pickupLon) || -123.0220;
        let dLat = parseFloat(options.deliveryLat) || 45.5152;
        let dLon = parseFloat(options.deliveryLon) || -122.6784;

        let activePinMode = options.activePinMode || 'pickup'; // 'pickup' or 'delivery'
        let routeLayer = null;

        const map = L.map(containerId, {
            scrollWheelZoom: true,
            zoomControl: true
        }).setView([pLat, pLon], 11);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener">OpenStreetMap</a>',
            maxZoom: 19
        }).addTo(map);

        // Pickup Marker (Draggable)
        const pickupMarker = L.marker([pLat, pLon], {
            icon: self.createIcon('pickup'),
            draggable: true,
            title: 'Drag to adjust pickup location'
        }).addTo(map);

        pickupMarker.bindPopup('<strong>📦 Pickup Location</strong><br/><small class="text-muted">Drag pin or click map to move</small>');

        // Delivery Marker (Draggable)
        const deliveryMarker = L.marker([dLat, dLon], {
            icon: self.createIcon('delivery'),
            draggable: true,
            title: 'Drag to adjust delivery destination'
        }).addTo(map);

        deliveryMarker.bindPopup('<strong>📍 Delivery Destination</strong><br/><small class="text-muted">Drag pin or click map to move</small>');

        function redrawRoute(geometryJson) {
            if (routeLayer) {
                try { map.removeLayer(routeLayer); } catch (e) { }
            }

            const curPLat = pickupMarker.getLatLng().lat;
            const curPLon = pickupMarker.getLatLng().lng;
            const curDLat = deliveryMarker.getLatLng().lat;
            const curDLon = deliveryMarker.getLatLng().lng;

            let drawn = false;
            if (geometryJson) {
                try {
                    const geoData = typeof geometryJson === 'string' ? JSON.parse(geometryJson) : geometryJson;
                    routeLayer = L.geoJSON(geoData, {
                        style: { color: '#4f46e5', weight: 5, opacity: 0.85 }
                    }).addTo(map);
                    drawn = true;
                } catch (e) { }
            }

            if (!drawn) {
                routeLayer = L.polyline([[curPLat, curPLon], [curDLat, curDLon]], {
                    color: '#4f46e5',
                    weight: 4,
                    dashArray: '6, 8',
                    opacity: 0.8
                }).addTo(map);
            }

            try {
                const group = new L.featureGroup([pickupMarker, deliveryMarker, routeLayer]);
                map.fitBounds(group.getBounds().pad(0.18));
            } catch (e) { }
        }

        // Pickup drag event
        pickupMarker.on('dragend', function (e) {
            const pos = e.target.getLatLng();
            self.reverseGeocode(pos.lat, pos.lng, function (addr) {
                if (options.onPickupChanged) {
                    options.onPickupChanged(pos.lat, pos.lng, addr);
                }
            });
        });

        // Delivery drag event
        deliveryMarker.on('dragend', function (e) {
            const pos = e.target.getLatLng();
            self.reverseGeocode(pos.lat, pos.lng, function (addr) {
                if (options.onDeliveryChanged) {
                    options.onDeliveryChanged(pos.lat, pos.lng, addr);
                }
            });
        });

        // Map click event based on activePinMode
        map.on('click', function (e) {
            const lat = e.latlng.lat;
            const lng = e.latlng.lng;

            if (activePinMode === 'pickup') {
                pickupMarker.setLatLng([lat, lng]);
                pickupMarker.openPopup();
                self.reverseGeocode(lat, lng, function (addr) {
                    if (options.onPickupChanged) {
                        options.onPickupChanged(lat, lng, addr);
                    }
                });
            } else {
                deliveryMarker.setLatLng([lat, lng]);
                deliveryMarker.openPopup();
                self.reverseGeocode(lat, lng, function (addr) {
                    if (options.onDeliveryChanged) {
                        options.onDeliveryChanged(lat, lng, addr);
                    }
                });
            }
        });

        setTimeout(function () {
            map.invalidateSize();
            redrawRoute(options.geometryJson || null);
        }, 200);

        const api = {
            map: map,
            pickupMarker: pickupMarker,
            deliveryMarker: deliveryMarker,
            setPinMode: function (mode) {
                activePinMode = mode;
            },
            setPickupLocation: function (lat, lon, zoomTo) {
                pickupMarker.setLatLng([lat, lon]);
                if (zoomTo) map.setView([lat, lon], 14);
            },
            setDeliveryLocation: function (lat, lon, zoomTo) {
                deliveryMarker.setLatLng([lat, lon]);
                if (zoomTo) map.setView([lat, lon], 14);
            },
            updateRouteGeometry: function (geometryJson) {
                redrawRoute(geometryJson);
            }
        };

        this.mapInstances[containerId] = api;
        return api;
    },

    /**
     * Initializes Single Pin Address Picker Map (for CreateAddress & EditAddress)
     */
    initPickerMap: function (containerId, options) {
        const container = document.getElementById(containerId);
        if (!container || typeof L === 'undefined') return null;

        if (this.mapInstances[containerId]) {
            try { this.mapInstances[containerId].remove(); } catch (e) { }
            delete this.mapInstances[containerId];
        }

        const self = this;
        const lat = parseFloat(options.lat) || 44.0462;
        const lon = parseFloat(options.lon) || -123.0220;

        const map = L.map(containerId, {
            scrollWheelZoom: true,
            zoomControl: true
        }).setView([lat, lon], 14);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener">OpenStreetMap</a>',
            maxZoom: 19
        }).addTo(map);

        const marker = L.marker([lat, lon], {
            icon: self.createIcon(options.iconType || 'pickup'),
            draggable: true
        }).addTo(map);

        if (options.label) {
            marker.bindPopup('<strong>' + options.label + '</strong><br/><small class="text-muted">Drag pin or click map to move</small>').openPopup();
        }

        marker.on('dragend', function (e) {
            const pos = e.target.getLatLng();
            self.reverseGeocode(pos.lat, pos.lng, function (addr) {
                if (options.onLocationSelected) {
                    options.onLocationSelected(pos.lat, pos.lng, addr);
                }
            });
        });

        map.on('click', function (e) {
            const clickLat = e.latlng.lat;
            const clickLon = e.latlng.lng;
            marker.setLatLng([clickLat, clickLon]);
            self.reverseGeocode(clickLat, clickLon, function (addr) {
                if (options.onLocationSelected) {
                    options.onLocationSelected(clickLat, clickLon, addr);
                }
            });
        });

        setTimeout(function () { map.invalidateSize(); }, 200);

        const api = {
            map: map,
            marker: marker,
            setLocation: function (newLat, newLon, newLabel) {
                marker.setLatLng([newLat, newLon]);
                map.setView([newLat, newLon], 15);
                if (newLabel) {
                    marker.bindPopup('<strong>' + newLabel + '</strong>').openPopup();
                }
            }
        };

        this.mapInstances[containerId] = api;
        return api;
    },

    /**
     * Initializes Single Map with one static/draggable marker
     */
    initSingleMap: function (containerId, lat, lon, label) {
        return this.initPickerMap(containerId, {
            lat: lat,
            lon: lon,
            label: label,
            iconType: 'pickup'
        });
    },

    /**
     * Initializes Multi-Marker Fleet Map for Admin
     */
    initMultiMarkerMap: function (containerId, points) {
        const container = document.getElementById(containerId);
        if (!container || typeof L === 'undefined') return null;

        if (this.mapInstances[containerId]) {
            try { this.mapInstances[containerId].remove(); } catch (e) { }
            delete this.mapInstances[containerId];
        }

        const map = L.map(containerId, {
            scrollWheelZoom: true,
            zoomControl: true
        }).setView([44.0462, -123.0220], 12);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener">OpenStreetMap</a>',
            maxZoom: 19
        }).addTo(map);

        const markers = [];
        if (points && Array.isArray(points) && points.length > 0) {
            points.forEach(p => {
                const lat = parseFloat(p.lat);
                const lon = parseFloat(p.lon);
                if (!isNaN(lat) && !isNaN(lon) && (lat !== 0 || lon !== 0)) {
                    const icon = this.createIcon(p.type || 'pickup');
                    const marker = L.marker([lat, lon], { icon: icon }).addTo(map);

                    let popupHtml = '<div style="font-size:13px;"><strong>' + (p.title || 'Location') + '</strong>';
                    if (p.subtitle) {
                        popupHtml += '<br/><span style="color:#64748b;">' + p.subtitle + '</span>';
                    }
                    if (p.link) {
                        popupHtml += '<br/><a href="' + p.link + '" class="btn btn-sm btn-primary mt-1 text-white py-0 px-2" style="font-size:11px;">View Details &rarr;</a>';
                    }
                    popupHtml += '</div>';

                    marker.bindPopup(popupHtml);
                    markers.push(marker);
                }
            });

            if (markers.length > 0) {
                try {
                    const group = new L.featureGroup(markers);
                    map.fitBounds(group.getBounds().pad(0.2));
                } catch (e) { }
            }
        }

        setTimeout(function () { map.invalidateSize(); }, 200);
        this.mapInstances[containerId] = map;
        return map;
    }
};

// Add CSS keyframe animation for pulse
if (typeof document !== 'undefined') {
    const style = document.createElement('style');
    style.innerHTML = `
        @keyframes pds-pulse {
            0% { transform: scale(0.85); opacity: 0.8; }
            50% { transform: scale(1.4); opacity: 0.2; }
            100% { transform: scale(0.85); opacity: 0.8; }
        }
    `;
    document.head.appendChild(style);
}
