public class EnterCarCommand : ICommand
{
    private readonly PlayerCarController controller;

    public EnterCarCommand(PlayerCarController controller)
    {
        this.controller = controller;
    }

    public void Execute()
    {
        if (controller == null) return;
        controller.EnterCar();
    }
}
