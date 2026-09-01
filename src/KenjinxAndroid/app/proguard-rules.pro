-optimizationpasses 5
-optimizations !code/simplification/arithmetic
-optimizations !code/simplification/cast
-optimizations !field/*
-optimizations !class/merging/*
-optimizations !method/inlining/short
-optimizations !method/inlining/unique
-optimizations method/inlining/tailrecursion
-optimizations method/removal/parameter
-optimizations code/merging
-optimizations code/simplification/variable
-allowaccessmodification
-repackageclasses ''
-keepattributes Exceptions,InnerClasses,Signature,*Annotation*
-dontpreverify
-keep public class * extends android.app.Activity
-keep public class * extends android.app.Application
-keep public class * extends android.app.Service
-keep public class * extends android.content.BroadcastReceiver
-keep public class * extends android.content.ContentProvider
-keep public class * extends androidx.fragment.app.Fragment
-dontwarn java.awt.Component
-dontwarn java.awt.GraphicsEnvironment
-dontwarn java.awt.HeadlessException
-dontwarn java.awt.Window
-dontwarn javax.lang.model.element.Modifier
-assumenosideeffects class java.lang.Math {
    public static double random();
    public static double sin(...);
    public static double cos(...);
    public static double sqrt(...);
}
-assumenosideeffects public class ** {
  public boolean is*();
  public boolean get*();
  public boolean has*();
}
