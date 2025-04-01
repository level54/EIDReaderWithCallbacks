
using EIDWebAPI.Settings;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.IO.Ports;

namespace EIDWebAPI.Workers;

internal class SerialWorker : IHostedService
{
    private const string c_EOL = "\r\n";
    private Queue<object> serial_data;

    private readonly ILogger<SerialWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IDispatcher _dispatcher;
    private CancellationTokenSource _source;
    private SerialPort? _serialPort;

    public SerialWorker(ILogger<SerialWorker> logger, IConfiguration config, IDispatcher dispatcher)
    {
        _logger = logger;
        _configuration = config;
        _dispatcher = dispatcher;
        _source = new();
        serial_data = new Queue<object>();

        _serialPort = new();
        if (_serialPort != null)
        {
            _serialPort.DataReceived += _serialPort_DataReceived;
        }
    }
    ~ SerialWorker()
    {
        if (_serialPort != null)
        {
            if (_serialPort.IsOpen)
                _serialPort.Close();

            _serialPort.DataReceived -= _serialPort_DataReceived;
            _serialPort.Dispose();
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Starting background service : SerialWorker");
        }

        if (!_serialPort.IsOpen)
        {
            try
            {
                var settings = _configuration.GetSection("SerialPort").Get<SerialPortSettings>();

                // Allow the user to set the appropriate properties.
                _serialPort.PortName = settings?.SerialPortName;
                _serialPort.BaudRate = (int)(settings?.SerialPortBaudRate);
                _serialPort.Parity = (Parity)Convert.ToInt32(settings?.SerialPortParity);
                _serialPort.DataBits = (int)(settings?.SerialPortDataBits);
                _serialPort.StopBits = (StopBits)Convert.ToInt32(settings?.SerialPortStopBits);
                _serialPort.Handshake = (Handshake)Convert.ToInt32(settings?.SerialPortHandshake);

                // Set the read/write timeouts
                _serialPort.ReadTimeout = (int)(settings?.SerialPortReadTimeout);
                _serialPort.WriteTimeout = (int)(settings?.SerialPortWriteTimeout);

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug($"PortName={_serialPort.PortName}");
                    _logger.LogDebug($"BaudRate={_serialPort.BaudRate}");
                    _logger.LogDebug($"Parity={_serialPort.Parity}");
                    _logger.LogDebug($"DataBits={_serialPort.DataBits}");
                    _logger.LogDebug($"StopBits={_serialPort.StopBits}");
                    _logger.LogDebug($"Handshake={_serialPort.Handshake}");
                    _logger.LogDebug($"ReadTimeout={_serialPort.ReadTimeout}");
                    _logger.LogDebug($"WriteTimeout={_serialPort.WriteTimeout}");
                }

                _serialPort.Encoding = new System.Text.ASCIIEncoding();
                _serialPort.NewLine = c_EOL;
                _serialPort.Open();

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

        if (_serialPort != null && _serialPort.IsOpen)
        {
            try
            {
                if (_serialPort != null)
                {
                    _serialPort.Close();
                    _serialPort.Dispose();
                    _serialPort = null;
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
            if (_serialPort != null && serial_data != null)
            {
                ProcessSerialPortData(_serialPort.ReadLine());
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
                    //((SrvEIDEvents)class_factory.ServerEvents).OnEIDScanResult(this, new Shared.EIDScanResultEventArgs(serial_data.Dequeue().ToString()));
                    var data = serial_data.Dequeue().ToString();
                    _logger.LogInformation(data);
                    _dispatcher.OnEventReceived(new EventReceivedArgs(data));
                }
            }

            Thread.Sleep((int)(settings?.SerialportReadDelay));
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
