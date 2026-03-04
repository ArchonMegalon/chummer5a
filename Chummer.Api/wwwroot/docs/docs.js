(function () {
  const OPENAPI_URL = "/openapi/v1.json";
  const METHOD_ORDER = ["get", "post", "put", "patch", "delete", "options", "head"];

  const statusEl = document.getElementById("status");
  const operationsEl = document.getElementById("operations");
  const apiKeyInput = document.getElementById("api-key");
  const reloadButton = document.getElementById("load-spec");

  const STORAGE_KEY = "chummer.docs.apiKey";
  apiKeyInput.value = window.localStorage.getItem(STORAGE_KEY) || "";
  apiKeyInput.addEventListener("input", () => {
    window.localStorage.setItem(STORAGE_KEY, apiKeyInput.value || "");
  });

  reloadButton.addEventListener("click", () => {
    void loadSpec();
  });

  void loadSpec();

  async function loadSpec() {
    setStatus("Loading OpenAPI spec...", "warn");
    operationsEl.innerHTML = "";

    try {
      const response = await fetch(OPENAPI_URL, { cache: "no-store" });
      if (!response.ok) {
        throw new Error("Spec request failed with HTTP " + response.status);
      }

      const spec = await response.json();
      const operations = collectOperations(spec);
      if (operations.length === 0) {
        setStatus("Spec loaded, but no operations were found.", "warn");
        return;
      }

      for (const operation of operations) {
        operationsEl.appendChild(renderOperation(operation));
      }

      setStatus("Loaded " + operations.length + " operations from " + OPENAPI_URL + ".", "ok");
    } catch (error) {
      setStatus("Failed to load spec: " + toErrorMessage(error), "warn");
    }
  }

  function collectOperations(spec) {
    const paths = spec && typeof spec === "object" ? spec.paths || {} : {};
    const items = [];

    for (const path of Object.keys(paths).sort()) {
      const pathItem = paths[path] || {};
      const pathParameters = normalizeParameters(pathItem.parameters);

      for (const method of METHOD_ORDER) {
        const operation = pathItem[method];
        if (!operation) {
          continue;
        }

        const operationParameters = normalizeParameters(operation.parameters);
        const parameters = dedupeParameters(pathParameters.concat(operationParameters));

        items.push({
          method: method.toUpperCase(),
          path,
          summary: operation.summary || operation.operationId || "(no summary)",
          description: operation.description || "",
          operationId: operation.operationId || "",
          parameters,
          requestBody: operation.requestBody || null,
          security: operation.security || spec.security || []
        });
      }
    }

    return items;
  }

  function normalizeParameters(raw) {
    if (!Array.isArray(raw)) {
      return [];
    }

    return raw
      .filter((value) => value && typeof value === "object")
      .map((value) => ({
        name: String(value.name || ""),
        in: String(value.in || "query"),
        required: Boolean(value.required),
        description: String(value.description || ""),
        schemaType: value.schema && typeof value.schema.type === "string" ? value.schema.type : "string"
      }))
      .filter((value) => value.name.length > 0);
  }

  function dedupeParameters(parameters) {
    const seen = new Set();
    const deduped = [];

    for (const parameter of parameters) {
      const key = parameter.in + ":" + parameter.name;
      if (seen.has(key)) {
        continue;
      }

      seen.add(key);
      deduped.push(parameter);
    }

    return deduped;
  }

  function renderOperation(operation) {
    const details = document.createElement("details");
    details.className = "operation";

    const summary = document.createElement("summary");

    const method = document.createElement("span");
    method.className = "method";
    method.textContent = operation.method;
    summary.appendChild(method);

    const path = document.createElement("span");
    path.className = "path";
    path.textContent = operation.path;
    summary.appendChild(path);

    const line = document.createElement("span");
    line.className = "summary";
    line.textContent = operation.summary;
    summary.appendChild(line);

    details.appendChild(summary);

    const body = document.createElement("div");
    body.className = "operation-body";

    if (operation.description) {
      const description = document.createElement("p");
      description.className = "hint";
      description.textContent = operation.description;
      body.appendChild(description);
    }

    const meta = document.createElement("p");
    meta.className = "hint";
    meta.textContent = [
      operation.operationId ? "operationId: " + operation.operationId : "",
      Array.isArray(operation.security) && operation.security.length > 0 ? "security: required" : "security: not declared"
    ]
      .filter((text) => text.length > 0)
      .join(" | ");
    body.appendChild(meta);

    const parametersWrap = document.createElement("div");
    parametersWrap.className = "parameters";

    const parameterInputs = [];
    for (const parameter of operation.parameters) {
      const block = document.createElement("label");
      block.className = "parameter";

      const title = document.createElement("strong");
      title.textContent = parameter.name;
      block.appendChild(title);

      const metadata = document.createElement("span");
      metadata.className = "meta";
      metadata.textContent = parameter.in + " | " + parameter.schemaType + (parameter.required ? " | required" : "");
      block.appendChild(metadata);

      const input = document.createElement("input");
      input.type = "text";
      input.placeholder = parameter.description || (parameter.required ? "required" : "optional");
      block.appendChild(input);

      parameterInputs.push({ spec: parameter, input });
      parametersWrap.appendChild(block);
    }

    if (operation.parameters.length > 0) {
      body.appendChild(parametersWrap);
    }

    let requestBodyInput = null;
    if (hasJsonRequestBody(operation.requestBody)) {
      const requestSection = document.createElement("div");
      requestSection.className = "request-controls";

      const requestLabel = document.createElement("strong");
      requestLabel.textContent = "JSON request body";
      requestSection.appendChild(requestLabel);

      const textarea = document.createElement("textarea");
      textarea.value = "{}";
      requestSection.appendChild(textarea);

      requestBodyInput = textarea;
      body.appendChild(requestSection);
    }

    const run = document.createElement("button");
    run.type = "button";
    run.className = "run";
    run.textContent = "Run request";
    body.appendChild(run);

    const response = document.createElement("pre");
    response.textContent = "Run a request to see response output.";
    body.appendChild(response);

    run.addEventListener("click", async () => {
      response.textContent = "Running...";
      try {
        const request = buildRequest(operation, parameterInputs, requestBodyInput);
        const payload = await executeRequest(request);
        response.textContent = payload;
      } catch (error) {
        response.textContent = "Request failed: " + toErrorMessage(error);
      }
    });

    details.appendChild(body);
    return details;
  }

  function hasJsonRequestBody(requestBody) {
    if (!requestBody || typeof requestBody !== "object") {
      return false;
    }

    const content = requestBody.content;
    return Boolean(content && (content["application/json"] || content["application/*+json"]));
  }

  function buildRequest(operation, parameterInputs, requestBodyInput) {
    let resolvedPath = operation.path;
    const query = new URLSearchParams();

    for (const entry of parameterInputs) {
      const value = entry.input.value.trim();
      if (entry.spec.required && value.length === 0) {
        throw new Error("Missing required " + entry.spec.in + " parameter: " + entry.spec.name);
      }

      if (value.length === 0) {
        continue;
      }

      if (entry.spec.in === "path") {
        resolvedPath = resolvedPath.replace("{" + entry.spec.name + "}", encodeURIComponent(value));
      } else if (entry.spec.in === "query") {
        query.set(entry.spec.name, value);
      }
    }

    const requestUrl = new URL(resolvedPath, window.location.origin);
    for (const [key, value] of query.entries()) {
      requestUrl.searchParams.append(key, value);
    }

    const headers = {
      Accept: "application/json"
    };

    const apiKey = apiKeyInput.value.trim();
    if (apiKey.length > 0) {
      headers["X-Api-Key"] = apiKey;
    }

    const request = {
      method: operation.method,
      url: requestUrl,
      options: {
        method: operation.method,
        headers
      }
    };

    if (requestBodyInput && !["GET", "HEAD"].includes(operation.method)) {
      const rawBody = requestBodyInput.value.trim();
      if (rawBody.length > 0) {
        headers["Content-Type"] = "application/json";
        try {
          JSON.parse(rawBody);
        } catch {
          throw new Error("Request body must be valid JSON.");
        }

        request.options.body = rawBody;
      }
    }

    return request;
  }

  async function executeRequest(request) {
    const response = await fetch(request.url, request.options);
    const contentType = response.headers.get("content-type") || "";
    const text = await response.text();

    let formattedBody = text;
    if (contentType.includes("application/json") && text.length > 0) {
      try {
        formattedBody = JSON.stringify(JSON.parse(text), null, 2);
      } catch {
        formattedBody = text;
      }
    }

    return [
      request.method + " " + request.url,
      "HTTP " + response.status + " " + response.statusText,
      "",
      formattedBody || "(empty response body)"
    ].join("\n");
  }

  function setStatus(text, level) {
    statusEl.className = "status " + (level || "");
    statusEl.textContent = text;
  }

  function toErrorMessage(error) {
    if (!error) {
      return "unknown error";
    }

    if (typeof error === "string") {
      return error;
    }

    if (error instanceof Error && typeof error.message === "string") {
      return error.message;
    }

    return String(error);
  }
})();
