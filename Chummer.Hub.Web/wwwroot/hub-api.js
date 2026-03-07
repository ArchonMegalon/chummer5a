window.chummerHubApi = Object.freeze({
    async send(path, method, payload) {
        const options = {
            method: method || "GET",
            credentials: "same-origin",
            headers: {
                "Accept": "application/json"
            }
        };

        if (payload !== null && payload !== undefined) {
            options.headers["Content-Type"] = "application/json";
            options.body = JSON.stringify(payload);
        }

        const response = await fetch(path, options);
        const text = await response.text();
        return JSON.stringify({
            status: response.status,
            text
        });
    }
});
