# OpenLobby

A Simple .NET console server for hosting, searching, and joining public game lobbies on the Internet.
Work is in progress!

# The Basics

* Lobby - Has a name, ID, clients list, IP:Port, search visibility, and password.

* Post Lobby - Clients send their info and server creates a Lobby using the info.

* Lobby Search - Clients send Lobby queries with query parameters, i.e. Lobby name.

* Join Lobby - Clients receive Lobby host's IP:Port if they provide the correct password for the requested Lobby.

* Disconnections - The Server is notified when a client disconnects.
