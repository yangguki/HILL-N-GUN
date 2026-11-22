using System;

namespace HillNGun.Proto.Greybox
{
    public static class Proto_GreyboxViewPresenterRegistry
    {
        static Func<IProto_GreyboxViewPresenter> s_factory;
        static IProto_GreyboxViewPresenter s_instance;

        public static bool HasFactory => s_factory != null;

        public static void RegisterFactory(Func<IProto_GreyboxViewPresenter> factory) => s_factory = factory;

        public static IProto_GreyboxViewPresenter Ensure()
        {
            if (s_factory == null)
                return null;
            if (s_instance == null)
                s_instance = s_factory();
            return s_instance;
        }

        public static void ResetForTests()
        {
            s_instance = null;
        }

        public static void ClearFactoryForTests()
        {
            s_factory = null;
            s_instance = null;
        }
    }
}