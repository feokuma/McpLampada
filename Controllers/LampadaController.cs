using System.Device.Gpio;

namespace McpLampada.Controllers;

public class LampadaController
{
    private readonly GpioController _controller = new();
    private readonly int _pin;
    private bool _ligada;

    public LampadaController(int pin)
    {
        _pin = pin;

        _controller.OpenPin(_pin, PinMode.Output);
        _controller.Write(_pin, PinValue.Low);
        _ligada = false;
    }

    public void Ligar()
    {
        _controller.Write(_pin, PinValue.Low);
        _ligada = true;
    }

    public void Desligar()
    {
        _controller.Write(_pin, PinValue.High);
        _ligada = false;
    }

    public bool Status() => _ligada;
}
