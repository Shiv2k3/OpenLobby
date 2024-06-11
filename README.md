# OpenLobby

A Simple .NET console server for hosting, searching, and joining public game lobbies on the Internet.
Work is in progress!

# The Basics

* Host Lobby - Clients send Lobby info and server creates a Lobby using the info.

* Search Lobbies - Clients send Lobby queries with query parameters and server will send back results.

* Join Lobby - Clients receive Lobby host's IP:Port if they provide the correct password for the requested Lobby.

* Disconnections - The Server is notified when a client disconnects and the Lobby will be closed when all clients (including host) disconnect.
