# Chaka-Proxy
Proxy Server for CSCI 415 - Networking and Parallel Computing class.

# Features
* Logging – Logs files to a file labeled proxy (date).log 
* Get Requests – Accepts TCP GET requests
* Safe Multithreading – Accepts each request on a separate thread and logs the requests safely
* Caching – Caches each request and will use the cache if the request is made again 

#How to Run
1.	Download the most recent release of the proxy.
2.	Run Chaka_Proxy.exe
3.	Specify the IP you want to listen to (leave blank for localhost)
4.	Specify the port to listen to (leave blank for 8880)
5.	Enjoy the proxy! 
