# DayCast

A Windows-based Google casting application.

# Description

This application will let you cast any \*.mp4 file from your home network to any Google device, such as a Chromecast or Google Home.

Simply compile it and run the DayCastClient.exe or use the pre-compiled one in the Build folder! The DayCastServer instance should be properly started and stopped automatically when the client is opened/closed.

Features include queueing up files, changing playback rate and full reconnection if closed. 

The local DayCastClient config file can be updated with a specific local IP address and Port used for the server communication (default being 5121). The server also supports an HTTP action to enqueue files or folders, with an optional minimum date for, useful for automation or smart home integration! (i.e. http://192.168.0.101:5121/D%3A%5CHomeSec%5Cvideo%5CIPC-LivingRoom%5C2_2018-09-09_17-03-56.mp4). Have a look at DayCastServer's QueueControlller.cs if you want to see all of it's functionality.

Feel free to fork the project and send me a pull request if you have any improvements!

# Credits

Big thanks to kakone for his work on GoogleCast found <a href="https://github.com/kakone/GoogleCast">here</a>!

# Donate

If you appreciate my work and feel like helping me realize other projects, you can donate at <a href="https://paypal.me/MattMckenzy">https://paypal.me/MattMckenzy</a>!
