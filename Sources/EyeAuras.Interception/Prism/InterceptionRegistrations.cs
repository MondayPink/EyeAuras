using WindowsInput;
using Unity;
using Unity.Extension;

namespace EyeAuras.Interception.Prism
{
    public sealed class InterceptionRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container.RegisterFactory<IInputSimulatorEx>(WellKnownKeyboardSimulators.InputSimulator, x => x.Resolve<WindowsInputSimulator>());
            Container.RegisterFactory<IInputSimulatorEx>(WellKnownKeyboardSimulators.InterceptionDriver, x => x.Resolve<DriverBasedInputSimulator>());
            
            Container.RegisterFactory<IKeyboardSimulator>(WellKnownKeyboardSimulators.InputSimulator, x => x.Resolve<IInputSimulatorEx>(WellKnownKeyboardSimulators.InputSimulator).Keyboard);
            Container.RegisterFactory<IKeyboardSimulator>(WellKnownKeyboardSimulators.InterceptionDriver, x => x.Resolve<IInputSimulatorEx>(WellKnownKeyboardSimulators.InterceptionDriver).Keyboard);

            Container.RegisterFactory<IMouseSimulator>(WellKnownKeyboardSimulators.InputSimulator, x => x.Resolve<IInputSimulatorEx>(WellKnownKeyboardSimulators.InputSimulator).Mouse);
            Container.RegisterFactory<IMouseSimulator>(WellKnownKeyboardSimulators.InterceptionDriver, x => x.Resolve<IInputSimulatorEx>(WellKnownKeyboardSimulators.InterceptionDriver).Mouse);
            
            Container.RegisterFactory<IInputSimulatorEx>( x => x.Resolve<IInputSimulatorEx>(WellKnownKeyboardSimulators.InputSimulator));
            Container.RegisterFactory<IKeyboardSimulator>( x => x.Resolve<IKeyboardSimulator>(WellKnownKeyboardSimulators.InputSimulator));
            Container.RegisterFactory<IMouseSimulator>( x => x.Resolve<IMouseSimulator>(WellKnownKeyboardSimulators.InputSimulator));
        }
    }
}