# EID Reader with Callbacks

A .NET 8.0 solution consisting of two Web API applications that work together to read data from an RS232 serial port (EID reader) and provide real-time callbacks to registered endpoints.

## Architecture Overview

The solution implements a publisher-subscriber pattern where:
- **EIDWebAPI** reads from the serial port and publishes events
- **CallbackAPI** subscribes to receive callbacks and processes the data

## Components

### 1. EIDWebAPI
- **Purpose**: Serial port communication and event broadcasting
- **Technology**: ASP.NET Core Web API with Windows Service support
- **Key Features**:
  - Reads data from configurable RS232 serial port
  - Manages callback endpoint registrations
  - Broadcasts received data to all registered callbacks
  - Supports running as console app or Windows service

### 2. CallbackAPI  
- **Purpose**: Receives and processes callback data
- **Technology**: ASP.NET Core Web API
- **Key Features**:
  - Auto-registers with EIDWebAPI on startup
  - Receives callback data and writes to output file
  - Provides manual registration/deregistration endpoints

## Prerequisites

- .NET 8.0 Runtime or SDK
- Windows OS (for System.IO.Ports and Windows Service support)
- RS232/EID reader device connected to a COM port
- Administrative privileges (for Windows Service installation and log directory access)

## Installation & Setup

### Step 1: Build the Applications
```bash
# Clone the repository
git clone <https://github.com/level54/EIDReaderWithCallbacks.git>
cd EIDReaderWithCallbacks

# Build both applications
dotnet build EIDWebAPI/EIDWebAPI.csproj --configuration Release
dotnet build CallbackAPI/CallbackAPI.csproj --configuration Release

# Publish for deployment
dotnet publish EIDWebAPI/EIDWebAPI.csproj --configuration Release --output ./publish/EIDWebAPI
dotnet publish CallbackAPI/CallbackAPI.csproj --configuration Release --output ./publish/CallbackAPI
```

### Step 2: Configure EIDWebAPI

Edit `EIDWebAPI/appsettings.json`:

```json
{
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
  "Kestrel": {
    "Endpoints": {
      "http": {
        "Url": "http://*:55555"
      }
    }
  },
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File"],
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "%ProgramData%//Level54//Meatplant//Logs//EIDWebAPI-.log",
          "rollingInterval": "Day",
          "fileSizeLimitBytes": 10485760,
          "rollOnFileSizeLimit": true
        }
      }
    ]
  }
}
```

### Step 3: Configure CallbackAPI

Edit `CallbackAPI/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "EIDWebAPI": "http://localhost:55555"
  },
  "Kestrel": {
    "Endpoints": {
      "http": {
        "Url": "http://*:44444"
      }
    }
  },
  "OutputFile": {
    "Name": "C:\\Tools\\output.txt"
  },
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File"],
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "%ProgramData%//Level54//Meatplant//Logs//CallbackAPI-.log",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

## Running the Applications

### Option 1: Console Applications

```bash
# Terminal 1 - Start EIDWebAPI
cd EIDWebAPI
dotnet run

# Terminal 2 - Start CallbackAPI  
cd CallbackAPI
dotnet run
```

### Option 2: Windows Service (EIDWebAPI only)

```bash
# Install as Windows Service
sc create EIDWebAPI binPath="C:\path\to\EIDWebAPI.exe"
sc start EIDWebAPI

# Check service status
sc query EIDWebAPI

# Stop and remove service
sc stop EIDWebAPI
sc delete EIDWebAPI
```

## API Endpoints

### EIDWebAPI Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/Event/RegisterCallback` | Register a callback URL |
| POST | `/api/Event/RemoveCallback` | Remove a registered callback |

#### Register Callback Request
```json
{
  "CallbackUrl": "http://localhost:44444/api/Callback/ReceiveCallback"
}
```

### CallbackAPI Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/Callback/RegisterMeForCallback` | Auto-register with EIDWebAPI |
| POST | `/api/Callback/DeregisterMeForCallback` | Remove callback registration |
| POST | `/api/Callback/ReceiveCallback` | Receives callback data (internal) |

## Usage Examples

### Example 1: Test with Swagger UI

1. Start both applications
2. Open browser to `http://localhost:55555/swagger` (EIDWebAPI)
3. Open browser to `http://localhost:44444/swagger` (CallbackAPI)
4. Use CallbackAPI's "RegisterMeForCallback" endpoint
5. Trigger data on EID reader device
6. Check output file for received data

### Example 2: Custom Callback Client

Create a simple console application to receive callbacks:

```csharp
using Microsoft.AspNetCore.Mvc;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();

app.MapPost("/api/receiver", async ([FromBody] CallbackData data) =>
{
    Console.WriteLine($"Received: {data.EventData}");
    
    // Process the EID data
    await File.AppendAllTextAsync("received_data.txt", 
        $"{DateTime.Now}: {data.EventData}{Environment.NewLine}");
    
    return Results.Ok();
});

app.Run("http://localhost:8080");

public class CallbackData
{
    public string EventData { get; set; }
}
```

Register this client with EIDWebAPI:

```csharp
using System.Text;
using System.Net.Http.Json;

var client = new HttpClient();
var callbackData = new { CallbackUrl = "http://localhost:8080/api/receiver" };

var response = await client.PostAsJsonAsync(
    "http://localhost:55555/api/Event/RegisterCallback", 
    callbackData);

Console.WriteLine($"Registration: {response.StatusCode}");
```

### Example 3: PowerShell Test Script

```powershell
# Register a callback endpoint
$callbackUrl = "http://localhost:9999/receive"
$eidApiUrl = "http://localhost:55555/api/Event/RegisterCallback"

$body = @{
    CallbackUrl = $callbackUrl
} | ConvertTo-Json

Invoke-RestMethod -Uri $eidApiUrl -Method Post -Body $body -ContentType "application/json"

Write-Host "Callback registered at $callbackUrl"

# Create a simple HTTP listener to test callbacks
$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add("http://localhost:9999/")
$listener.Start()

Write-Host "Listening for callbacks on http://localhost:9999/"

while ($true) {
    $context = $listener.GetContext()
    $request = $context.Request
    $response = $context.Response
    
    if ($request.HttpMethod -eq "POST" -and $request.Url.LocalPath -eq "/receive") {
        $reader = new StreamReader($request.InputStream)
        $data = $reader.ReadToEnd()
        Write-Host "Received callback: $data"
        
        $buffer = [System.Text.Encoding]::UTF8.GetBytes("OK")
        $response.ContentLength64 = $buffer.Length
        $response.OutputStream.Write($buffer, 0, $buffer.Length)
    }
    
    $response.Close()
}
```

## Data Flow

1. **EID Reader Device** → RS232 serial port → **EIDWebAPI**
2. **EIDWebAPI** reads serial data and triggers `OnEventReceived`
3. **Dispatcher** broadcasts event to `EventController`
4. **EventController** calls `CallbackManager.InvokeCallbacksAsync()`
5. **CallbackManager** POSTs JSON data to all registered callback URLs
6. **CallbackAPI** receives data at `/api/Callback/ReceiveCallback`
7. **CallbackAPI** processes data and writes to configured output file

### Callback Data Format

```json
{
  "EventData": "RAW_EID_DATA_FROM_SERIAL_PORT"
}
```

## Troubleshooting

### Common Issues

**Issue: Serial port access denied**
```
Solution: Run as Administrator or ensure user has COM port access rights
```

**Issue: Callback registration fails**
```
Solution: 
- Verify EIDWebAPI is running on correct port
- Check firewall settings
- Ensure callback URL is accessible from EIDWebAPI
```

**Issue: No data received**
```
Solution:
- Verify EID reader device is connected and powered
- Check COM port configuration matches device settings
- Use serial port monitoring tool to verify data transmission
```

**Issue: Windows Service won't start**
```
Solution:
- Check Event Viewer for detailed error messages
- Ensure appsettings.json is in same directory as executable
- Verify log directory permissions
```

### Debug Logging

Enable debug logging by modifying appsettings.json:

```json
{
  "Serilog": {
    "MinimumLevel": "Debug"
  }
}
```

### Log Files Locations

- **EIDWebAPI**: `C:\ProgramData\Level54\Meatplant\Logs\EIDWebAPI-{date}.log`
- **CallbackAPI**: `C:\ProgramData\Level54\Meatplant\Logs\CallbackAPI-{date}.log`

## Configuration Reference

### Serial Port Settings

| Parameter | Default | Description |
|-----------|---------|-------------|
| SerialPortName | COM2 | COM port identifier |
| SerialPortBaudRate | 115200 | Communication speed |
| SerialPortParity | 0 | Parity check (0=None, 1=Odd, 2=Even) |
| SerialPortDataBits | 8 | Data bits per frame |
| SerialPortStopBits | 1 | Stop bits (1=1, 2=2, 3=1.5) |
| SerialPortHandshake | 0 | Handshake protocol (0=None, 1=XOnXOff, 2=RequestToSend, 3=RequestToSendXOnXOff) |
| SerialPortReadTimeout | 500ms | Read timeout in milliseconds |
| SerialPortWriteTimeout | 500ms | Write timeout in milliseconds |
| SerialportWriteSubmitDelay | 50ms | Delay after write operations |
| SerialportReadDelay | 50ms | Delay between read operations |

## Development Notes

### Project Structure

```
EIDReaderWithCallbacks/
├── EIDWebAPI/
│   ├── Controllers/
│   │   └── EventController.cs          # Callback registration endpoints
│   ├── Workers/
│   │   └── SerialWorker.cs             # Serial port background service
│   ├── Classes/
│   │   ├── CallbackManager.cs          # Manages callback URLs
│   │   ├── Dispatcher.cs               # Event dispatcher
│   │   └── IDispatcher.cs              # Dispatcher interface
│   ├── Settings/
│   │   ├── SerialPortSettings.cs       # Serial port configuration
│   │   └── SwaggerSettings.cs          # Swagger UI settings
│   └── Program.cs                      # Application entry point
└── CallbackAPI/
    ├── Controllers/
    │   └── CallbackController.cs        # Callback receiver endpoints
    ├── Settings/
    │   ├── OutputFileSettings.cs        # Output file configuration
    │   └── SwaggerSettings.cs
    ├── Models/
    │   └── EventDataModel.cs            # Callback data model
    └── Program.cs
```

### Key Technologies

- **.NET 8.0** - Modern, high-performance framework
- **ASP.NET Core** - Web API framework
- **System.IO.Ports** - Serial port communication
- **Serilog** - Structured logging
- **Windows Services** - Production deployment option
- **Swagger/OpenAPI** - API documentation

## Security Considerations

1. **Network Security**: Callback URLs should use HTTPS in production
2. **Authentication**: Consider adding API keys or JWT authentication
3. **Input Validation**: Validate serial port data before processing
4. **Access Control**: Restrict who can register callbacks
5. **Logging**: Be careful not to log sensitive EID data

## Performance Considerations

- **Serial Port Buffering**: Adjust read/write delays based on device speed
- **Callback Timeouts**: Implement timeout handling for callback HTTP requests
- **Concurrent Callbacks**: Current implementation processes callbacks sequentially
- **Memory Management**: Monitor memory usage with high-frequency data

## License

Free

## Support

For support and questions:
- Check log files for error messages
- Verify configuration settings
- Test with Swagger UI endpoints
- Review this documentation for common solutions