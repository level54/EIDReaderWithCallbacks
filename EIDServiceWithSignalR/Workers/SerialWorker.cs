
using EIDServiceWithSignalR.Classes;
using EIDServiceWithSignalR.Hubs;
using EIDServiceWithSignalR.Settings;
using Newtonsoft.Json.Schema.Generation;
using Newtonsoft.Json.Serialization;
using System.IO.Ports;
using System.Net;

namespace EIDServiceWithSignalR.Workers;

internal class SerialWorker : IHostedService
{
    private const string c_EOL = "\r\n";
    private Queue<object> serial_data;

    private readonly ILogger<SerialWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IEventNotificationService _events;
    private CancellationTokenSource _source;
    private SerialPort? _serialPort;

    public SerialPort? SerialPort { get => _serialPort; set => _serialPort = value; }

    public SerialWorker(ILogger<SerialWorker> logger, IConfiguration config, IEventNotificationService events)
    {
        _logger = logger;
        _configuration = config;
        _events = events;
        _source = new();
        serial_data = new Queue<object>();

        SerialPort = new();
        if (SerialPort != null)
        {
            SerialPort.DataReceived += _serialPort_DataReceived;
        }
    }
    ~ SerialWorker()
    {
        if (SerialPort != null)
        {
            if (SerialPort.IsOpen)
                SerialPort.Close();

            SerialPort.DataReceived -= _serialPort_DataReceived;
            SerialPort.Dispose();
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Starting background service : SerialWorker");
        }

        if (!SerialPort.IsOpen)
        {
            try
            {
                var settings = _configuration.GetSection("SerialPort").Get<SerialPortSettings>();

                // Allow the user to set the appropriate properties.
                SerialPort.PortName = settings?.SerialPortName;
                SerialPort.BaudRate = (int)(settings?.SerialPortBaudRate ?? 0);
                SerialPort.Parity = (Parity)Convert.ToInt32(settings?.SerialPortParity);
                SerialPort.DataBits = (int)(settings?.SerialPortDataBits ?? 0);
                SerialPort.StopBits = (StopBits)Convert.ToInt32(settings?.SerialPortStopBits);
                SerialPort.Handshake = (Handshake)Convert.ToInt32(settings?.SerialPortHandshake);

                // Set the read/write timeouts
                SerialPort.ReadTimeout = (int)(settings?.SerialPortReadTimeout ?? 0);
                SerialPort.WriteTimeout = (int)(settings?.SerialPortWriteTimeout ?? 0);

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug($"PortName={SerialPort.PortName}");
                    _logger.LogDebug($"BaudRate={SerialPort.BaudRate}");
                    _logger.LogDebug($"Parity={SerialPort.Parity}");
                    _logger.LogDebug($"DataBits={SerialPort.DataBits}");
                    _logger.LogDebug($"StopBits={SerialPort.StopBits}");
                    _logger.LogDebug($"Handshake={SerialPort.Handshake}");
                    _logger.LogDebug($"ReadTimeout={SerialPort.ReadTimeout}");
                    _logger.LogDebug($"WriteTimeout={SerialPort.WriteTimeout}");
                }

                SerialPort.Encoding = new System.Text.ASCIIEncoding();
                SerialPort.NewLine = c_EOL;
                SerialPort.Open();

                //start the ProcessReceiveQueueData procedure on a seperate thread to allow code execution to continue
                //if we don't do this here, then the rest of the API will not function.

                var result = ThreadPool.QueueUserWorkItem(ProcessReceiveQueueData, _source.Token, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
        }



        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Stopping background service : SerialWorker");
        }

        //cancel the source object, which will stop the execution of ProcessReceiveQueueData
        if (_source != null) _source.Cancel();

        if (SerialPort != null && SerialPort.IsOpen)
        {
            try
            {
                if (SerialPort != null)
                {
                    SerialPort.Close();
                    SerialPort.Dispose();
                    SerialPort = null;
                }

                if (serial_data != null)
                    serial_data.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
        }

        return Task.CompletedTask;
    }

    private void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            if (SerialPort != null && serial_data != null)
            {
                ProcessSerialPortData(SerialPort.ReadLine());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
        }
    }

    private void ProcessSerialPortData(string data)
    {
        if (!string.IsNullOrEmpty(data))
        {
            lock (serial_data)
                serial_data.Enqueue(data);
        }
    }

    private void ProcessReceiveQueueData(CancellationToken cancellationToken)
    {

        var settings = _configuration.GetSection("SerialPort").Get<SerialPortSettings>();

        while (!cancellationToken.IsCancellationRequested)
        {
            if (serial_data.Count > 0)
            {
                lock (serial_data)
                {
                    var data = serial_data.Dequeue().ToString();
                    _logger.LogInformation(data);
                    //todo
                    //_dispatcher.OnEventReceived(new EventReceivedArgs(data));
                    var objData = new MyObject()
                    {
                        TimeStamp = DateTime.Now,
                        Name = Dns.GetHostName(),
                        Description = data,
                        Type = "SerialData"
                    };

                    // Create a JSON schema for MyObject automatically
                    // Create a JSchemaGenerator with custom settings
                    var generator = new JSchemaGenerator();
                    //{
                    //    // Apply a CamelCaseNamingStrategy to the ContractResolver
                    //    ContractResolver = new DefaultContractResolver
                    //    {
                    //        NamingStrategy = new CamelCaseNamingStrategy()
                    //    }
                    //};
                    var schema = generator.Generate(typeof(MyObject));

                    // Optionally, convert the schema to its JSON string representation:
                    string schemaJson = schema.ToString();

                    // Build a payload that includes both the data and its schema
                    var payload = new
                    {
                        Data = objData,
                        Schema = schemaJson
                    };

                    _events.NotifyClientsOfSerialDataReceivedAsync("DataReceived", payload);

                    _events.NotifyClientsAsync("ThreadSleep", $"The procedure will now rest.");
                }
            }

            Thread.Sleep((int)(settings?.SerialportReadDelay ?? 0));
        }

        if (cancellationToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("ProcessReceiveQueueData CancellationRequested");
            }
        }

    }

}
