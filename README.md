# OpenLobby

About Simple .NET console server for hosting, searching, and joining public game lobbies on the Internet.

# Basically...

* Lobby- Has a name, id, player list, IP:Port, search visibility, and password.

* Post Lobby- Clients send their info and server will create a lobby using that info.

* Lobby Search- Clients can query for lobbies with query parameters.

* Join Lobby- Clients receive IP:Port if they are allowed to join the lobby they requested from the server.

* Disconnections- The server should be notified when a player disconnects.

# The server's perspective

1. Client's request to host a lobby is is created and stored.

2. Client's request to search for lobbies are queried, and results are sent back.

3. Clients receive IP:Port if they are allowed to join the lobby they requested.

4. Host sends connected notification when a client joins. As a security measure, host can ask server if a joining client is authorized.

5. Host sends disconnected notification when a client or itself disconnects, and the lobby is removed once all the clients leave.

# Community 

You are free to contribute to the repository is your interested.
