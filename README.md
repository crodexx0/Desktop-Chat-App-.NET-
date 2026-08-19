# DevinChat

A .NET 8 desktop chat application: sign up / sign in with a password, add other users by
username, and exchange real-time messages with them.

- **`src/DevinChat.Server`** — ASP.NET Core minimal API + SignalR hub, EF Core / SQLite storage,
  JWT bearer authentication, PBKDF2-SHA256 password hashing.
- **`src/DevinChat.Client`** — Avalonia UI desktop client (Windows, macOS, Linux) using MVVM
  (CommunityToolkit.Mvvm) and the SignalR .NET client.

## Running

Terminal 1 — server (listens on http://localhost:5099, creates `devinchat.db` on first run):

```bash
dotnet run --project src/DevinChat.Server
```

Terminal 2 — desktop client:

```bash
dotnet run --project src/DevinChat.Client
```

Launch the client twice (or on two machines pointing at the same server) to chat between two
accounts. The server URL is editable on the login page, or can be set with the
`DEVINCHAT_SERVER` environment variable.

## Using it

1. Choose *Create an account*, pick a username (3+ chars) and password (6+ chars).
2. In the sidebar, type another user's username and press **Add**.
3. Select the contact and send messages; they arrive live over SignalR and are persisted, so
   history reloads when you select the conversation again.

## API

| Method | Route | Description |
| --- | --- | --- |
| POST | `/api/auth/register` | Create an account, returns a JWT |
| POST | `/api/auth/login` | Sign in, returns a JWT |
| GET | `/api/me` | Current user |
| GET | `/api/users/search?q=` | Find users by username / display name |
| GET / POST | `/api/contacts` | List / add contacts (adds both directions) |
| GET | `/api/messages/{otherUserId}` | Conversation history |
| Hub | `/hubs/chat` | `SendMessage(recipientId, text)`, `ReceiveMessage`, `PresenceChanged` |

Swagger UI is available at `/swagger` in the Development environment.

## Configuration

`src/DevinChat.Server/appsettings.json` holds the SQLite connection string and the JWT
issuer/audience/secret. **Replace `Jwt:Secret` with a long random value before deploying**, and
put the server behind HTTPS — the shipped defaults are for local development only.
