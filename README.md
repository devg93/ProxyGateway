
System Components and Workflow
The MySQL Proxy Gateway operates in a client-server architecture where the following components interact:

Docker Client: The source of MySQL requests. It sends requests to the proxy and waits for responses.

Proxy Server: Listens on a specific port (configured as proxyPort), accepts client connections, and forwards requests to the MySQL server.

MySQL Server: Receives forwarded requests from the proxy, executes them, and returns the results back to the proxy.

