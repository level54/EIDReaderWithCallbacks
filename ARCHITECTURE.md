# Architecture Diagram

## Text-Based Architecture Diagram

```
┌─────────────────┐    RS232     ┌─────────────────┐    HTTP     ┌─────────────────┐
│   EID Reader    │◄────────────►│    EIDWebAPI    │◄──────────►│   CallbackAPI   │
│   (Hardware)    │   Serial     │   (Publisher)   │  Callbacks │   (Subscriber)  │
└─────────────────┘    Port      └─────────────────┘    POST     └─────────────────┘
                                                │
                                                │
                                                ▼
                                        ┌─────────────────┐
                                        │   Output File   │
                                        │  (output.txt)   │
                                        └─────────────────┘

┌─────────────────┐
│   Other Apps    │◄─────────────────┐
│   (Optional)    │   HTTP Callbacks │
└─────────────────┘                   │
                                      │
┌─────────────────┐                   │
│   Web Client    │◄──────────────────┘
│   (Swagger UI)  │
└─────────────────┘
```

## Mermaid Diagram (for documentation tools)

```mermaid
graph LR
    A[EID Reader<br/>Hardware Device] -->|RS232<br/>Serial Port| B[EIDWebAPI<br/>Publisher Service]
    B -->|HTTP POST<br/>Callbacks| C[CallbackAPI<br/>Subscriber Service]
    C -->|File Write| D[Output File<br/>output.txt]
    B -->|HTTP POST<br/>Callbacks| E[Other Applications<br/>Optional]
    F[Web Client<br/>Swagger UI] -->|HTTP Requests| B
    F -->|HTTP Requests| C
    
    style A fill:#ff9999
    style B fill:#99ccff
    style C fill:#99ff99
    style D fill:#ffff99
    style E fill:#cc99ff
    style F fill:#ffcc99
```

## Data Flow Sequence Diagram

```mermaid
sequenceDiagram
    participant Reader as EID Reader
    participant EIDAPI as EIDWebAPI
    participant Dispatcher as Dispatcher
    participant CallbackMgr as CallbackManager
    participant CallbackAPI as CallbackAPI
    participant File as Output File

    Reader->>EIDAPI: Send data via RS232
    EIDAPI->>Dispatcher: OnEventReceived()
    Dispatcher->>EIDAPI: EventReceived event
    EIDAPI->>CallbackMgr: InvokeCallbacksAsync()
    CallbackMgr->>CallbackAPI: HTTP POST with data
    CallbackAPI->>File: Write received data
    CallbackAPI-->>CallbackMgr: 200 OK
```

## Recommended Diagram Tools

For creating professional architecture diagrams, consider using:

1. **Draw.io (diagrams.net)** - Free web-based tool
2. **Lucidchart** - Professional diagramming tool
3. **PlantUML** - Text-based diagram generation
4. **Mermaid.js** - If you're using Markdown with Mermaid support
5. **Visio** - Microsoft's diagramming tool
6. **PowerPoint** - For quick professional diagrams

## Quick Diagram Template

Here's a simple structure you can follow in any diagramming tool:

```
EID Reader (Hardware)
    ↓ [RS232 Serial Connection]
EIDWebAPI (Port 55555)
    ├── SerialWorker: Reads COM port
    ├── Dispatcher: Manages events
    ├── CallbackManager: Handles registrations
    └── EventController: REST API endpoints
    ↓ [HTTP POST Callbacks]
CallbackAPI (Port 44444)
    ├── Registers with EIDWebAPI
    ├── Receives callback data
    └── Writes to output.txt
    ↓ [File Output]
C:\Tools\output.txt
```

Would you like me to create a more detailed text-based diagram or provide specific instructions for using any of these tools?