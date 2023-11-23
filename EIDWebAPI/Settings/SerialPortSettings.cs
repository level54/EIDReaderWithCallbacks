namespace EIDWebAPI.Settings;

internal class SerialPortSettings
{
    public string? SerialPortName { get; set; }
    public int SerialPortBaudRate { get; set; }
    public int SerialPortParity { get; set; }
    public int SerialPortDataBits { get; set; }
    public int SerialPortStopBits { get; set; }
    public int SerialPortHandshake { get; set; }
    public int SerialPortReadTimeout { get; set; }
    public int SerialPortWriteTimeout { get; set; }
    public int SerialportWriteSubmitDelay { get; set; }
    public int SerialportReadDelay { get; set; }
}
