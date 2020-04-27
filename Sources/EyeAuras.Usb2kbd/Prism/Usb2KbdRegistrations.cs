using WindowsInput;
using EyeAuras.Interception;
using Unity;
using Unity.Extension;
using Unity.Lifetime;

namespace EyeAuras.Usb2kbd.Prism
{
    public sealed class Usb2KbdRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container.RegisterFactory<IInputSimulatorEx>(WellKnownKeyboardSimulators.Usb2Kbd, x => x.Resolve<Usb2KbdSimulator>(), new ContainerControlledLifetimeManager());
            Container.RegisterFactory<IKeyboardSimulator>(WellKnownKeyboardSimulators.Usb2Kbd, x => x.Resolve<IInputSimulatorEx>(WellKnownKeyboardSimulators.Usb2Kbd).Keyboard);
            Container.RegisterFactory<IMouseSimulator>(WellKnownKeyboardSimulators.Usb2Kbd, x => x.Resolve<IInputSimulatorEx>(WellKnownKeyboardSimulators.Usb2Kbd).Mouse);
        }
    }
}