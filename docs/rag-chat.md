# Step 4 chat configuration

Apply the `AddChatMessageDeletionAndSources` EF migration before enabling the new chat API. Existing indexed documents and the Step 2/3 retrieval endpoint are unchanged.

Configure these server-side environment variables (double underscores map to configuration colons):

| Variable | Purpose |
| --- | --- |
| `Chat__EncryptionKeyBase64` | Required stable base64 encoding of 32 random bytes. Back it up securely; losing or rotating it without re-encryption makes stored chat content unreadable. |
| `Chat__DepartmentMatchThreshold` | Minimum Qdrant cosine score for treating the best company-wide match as a relevant department-scoped document. Defaults to `0.70`; valid values are from `0` to `1`. |
| `Chat__GeminiApiKey` | Gemini generation key. If empty, the server uses the existing `Embedding__ApiKey`. |
| `Chat__GeminiModel` | Default Gemini model ID; defaults to `gemini-3.8-flash`. The chat selector also offers Gemini 3.6 Flash, Gemini 3.1 Flash Lite, and Gemini 2.5 Flash Lite when a Gemini key is configured. `gemini-2.5-flash` is no longer offered; an existing override with that value falls back to `gemini-3.8-flash`. |
| `Chat__ClaudeApiKey` | Claude generation key. Claude is hidden from the selector while this is empty. |
| `Chat__ClaudeBaseUrl` | Anthropic-compatible API base URL; defaults to `https://api.anthropic.com/v1`. For the gateway shown by the user, use `https://api.mwapi.dev/v1`. The server appends `/messages`. |
| `Chat__ClaudeModel` | Claude Sonnet model ID; defaults to `claude-sonnet-5`. |

Generate a key once with `([Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32)))` in PowerShell and store it in the deployment secret store. Do not commit it to appsettings. The configured chat key remains stable across deployments and restarts.

For local development with an Anthropic-compatible gateway, set `Chat:ClaudeBaseUrl` and `Chat:ClaudeApiKey` in the Web API user secrets. Use a newly rotated gateway key, and set `Chat:ClaudeModel` to a model ID that the gateway supports. This gateway receives the retrieved document passages used to generate each answer.

`GET /api/chat/models` lists enabled choices. The browser sends the selected model ID, a question, and a UUID request ID to `POST /api/chat/turns/stream`. Generation stays on the server and streams SSE events. A new conversation is created on its first message. Chat requests require an active organization membership; conversation and message reads are scoped to the signed-in owner. Old assistant answers are omitted when their cited chunks are no longer accessible.

Before generation, retrieval compares the best company-wide indexed match with the signed-in user's existing document access. If the best match is a department-scoped document, meets `Chat__DepartmentMatchThreshold`, and is not accessible under the existing membership, ownership, or grant rules, chat returns `No access` without loading that document's chunk content. Otherwise, the model receives only authorized passages. If no relevant company document matches, the existing no-source response is returned.
