Please note that the binaries cannot reside in the same folder, as both executables depend on an appsettings.json file, which is unique to each executable. For this POC to work, the two binaries must reside on the same computer.

# EIDWebAPI

The web API is the service that talks to the RS232 port. The executable can be run like a normal executable, and it will open a console windows and log it’s output to that. It can also be setup as a Windows service by making use of the [sc] command in Windows.

Before running the executable, open the appsettings.json file with a text editor like Notepad. Look for the SerialPort section and change te settings as required. The setting names under this section is exactly the same as in the Meatplant config file for the EID service. See Level54.Services.EID.exe.config

```
  "SerialPort": {
    "SerialPortName": "COM2",
    "SerialPortBaudRate": 115200,
    "SerialPortParity": 0,
    "SerialPortDataBits": 8,
    "SerialPortStopBits": 1,
    "SerialPortHandshake": 0,
    "SerialPortReadTimeout": 500,
    "SerialPortWriteTimeout": 500,
    "SerialportWriteSubmitDelay": 50,
    "SerialportReadDelay": 50
  },
```

Next, find the Kestrel section and set the URL to what ever port number you want to use. For more detail on how to setup this section, you can refer to this [Microsoft article](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-8.0).

```
  "Kestrel": {
    "Endpoints": {
      "http": {
        "Url": "http://*:55555"
      }
    }
  },
```

Please also note that the service will log it’s output to the console as well as to a log file under C:\ProgramData\Level54\Meatplant\Logs. The log file is named EIDWebAPI-{current date}.log

When the service is running, you can access it’s web interface via the URL under the Kestrel section followed by the path /swagger/index.html. So, with the setting as above, the final url would be http://{localhost or ip address or machine name or FQDN}:55555/swagger/index.html

The page should look like this.
![Screenshot of EIDWebAPI Swagger page](https://bitbucket.org/repo/7EG7zRy/images/983419939-Web%20capture_24-11-2023_173522_localhost.jpeg)
You don’t need to use any of these functions, but the web interface can be used to manually de/register a callback url. Note that any application or service that wants to call these functions, needs to pass the callback location using the CallbackModel object supplied. The schema for the service is also available at the location shown below the EIDWebAPI name.

## Inner Workings
The **SerialWorker** runs as a service and waits for incoming data from a RS232 serial port. It has a reference to the IDispatcher interface, and uses this interface to dispatch any new data via the Dispatcher's ```OnEventReceived``` function.
A remote entiry, registers it's own endpoint with EIDWebAPI by calling the *RegisterMeForCallback* API. The EventController class takes the endpoint and adds it to the CallbackManager class. The EventController class also registeres the Dispatcher's ```EventReceived``` event. Once an event fires through the dispatcher, the EventController calls the ```InvokeCallbacksAsync``` function on the CallbackManager and passes the relevant event data as a parameter.
When the CallbackManager's ```InvokeCallbacksAsync``` function is invoked, it takes the data received as a parameter, serializes it to ```application/json``` and post's it to each registered callback endpoint.

# CallbackAPI

This executable will register itself as a callback function with the EIDWebAPI and should reside on the same computer as the EIDWebAPI service, as it’s endpoint has been hard coded into the registration function for ease of coding.

The executable only runs as a normal console application, and will produce output to the console, and to a log file. It will also produce the result of a callback call to a text file.

Before running the executable, open the appsettings.json file with a text editor like Notepad.

Look for the ConnectionStrings section, and set the EIDWebAPI setting to the endpoint that you chose earlier for the EIDWebAPI service. Please note that this should ONLY be the endpoint, and should not include the /swagger/index.html parts.

```
  "ConnectionStrings": {
    "EIDWebAPI": "http://localhost:55555"
  },
```

Next, find the Kestrel section. Note that this will define where the callback resides, and I have hardcoded the registration of this endpoint in the code, so do not change this location. In a production environment, the idea would be to be able to modify this to what ever you need it to be.

```
  "Kestrel": {
    "Endpoints": {
      "http": {
        "Url": "http://*:44444"
      }
    }
  },
```

Next, find the OutputFile section, and set the Name setting to a file location where the output from a callback call will be saved.

```
  "OutputFile": {
    "Name": "C:\\Tools\\output.txt"
  },
```

Please also note that the service will log it’s output to the console as well as to a log file under C:\ProgramData\Level54\Meatplant\Logs. The log file is named CallbackAPI-{current date}.log

When the executable is running, you can access it’s web interface via the URL under the Kestrel section following by the path /swagger/index.html. So, with the setting as above, the final url would be http://{localhost or ip address or machine name or FQDN}:44444/swagger/index.html

The page should look like this.
![Screenshot of CallbackAPI Swagger page](https://bitbucket.org/repo/7EG7zRy/images/3990311434-Web%20capture_24-11-2023_18039_localhost.jpeg)
To register the CallbackAPI application with the EIDWebAPI service for callback, use the RegisterMeForCallback function.

As soon as this is done, the CallbackAPI application will start receiving the callback data from the EIDWebAPI service. The Swagger web interface will not indicate any updates, and the ReceiveCallback function is actually the entpoint that is registered with the EIDWebAPI service.

Any data received via the callback, will be saved to the OutputFile, and you can verify this by opening the file in a self updating text editor like Notepad++. You can also verify that data is indeed writable to the file, by running the ReceiveCallback function manually.
