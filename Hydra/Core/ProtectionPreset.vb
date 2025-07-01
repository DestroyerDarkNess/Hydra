Imports Newtonsoft.Json

Public Class ProtectionPreset

    <JsonProperty("name")>
    Public Property Name As String = ""

    <JsonProperty("description")>
    Public Property Description As String = ""

    <JsonProperty("version")>
    Public Property Version As String = "1.0"

    <JsonProperty("created")>
    Public Property Created As DateTime = DateTime.Now

    ' Configuraciones del Renamer
    <JsonProperty("renamer")>
    Public Property Renamer As New RenamerSettings()

    ' Configuraciones de Protección
    <JsonProperty("protections")>
    Public Property Protections As New ProtectionSettings()

    ' Configuraciones del Packer
    <JsonProperty("packer")>
    Public Property Packer As New PackerSettings()

    ' Configuraciones de Salida
    <JsonProperty("output")>
    Public Property Output As New OutputSettings()

    ' Configuraciones de VM
    <JsonProperty("vm")>
    Public Property VM As New VMSettings()

    ' Configuraciones Anti
    <JsonProperty("anti")>
    Public Property Anti As New AntiSettings()

    ' Configuraciones de DLL
    <JsonProperty("dll")>
    Public Property DLL As New DLLSettings()
    
    ' Configuraciones de EntryPoint
    <JsonProperty("entryPoint")>
    Public Property EntryPoint As New EntryPointSettings()

End Class

Public Class RenamerSettings

    <JsonProperty("enabled")>
    Public Property Enabled As Boolean = False

    <JsonProperty("engine")>
    Public Property Engine As Integer = 0 ' 0 = dnlib, 1 = AsmResolver

    <JsonProperty("mode")>
    Public Property Mode As Integer = 0 ' BaseMode

    <JsonProperty("baseChars")>
    Public Property BaseChars As String = "ABCDEFGHIJKLMNÑOPQRSTUVWXYZ0123456789"

    <JsonProperty("length")>
    Public Property Length As Integer = 10

    <JsonProperty("tag")>
    Public Property Tag As String = ""

    <JsonProperty("resources")>
    Public Property Resources As Boolean = False

    <JsonProperty("namespace")>
    Public Property NamespaceEx As Boolean = False

    <JsonProperty("namespaceEmpty")>
    Public Property NamespaceEmpty As Boolean = False

    <JsonProperty("className")>
    Public Property ClassName As Boolean = False

    <JsonProperty("methods")>
    Public Property Methods As Boolean = False

    <JsonProperty("properties")>
    Public Property Properties As Boolean = False

    <JsonProperty("fields")>
    Public Property Fields As Boolean = False

    <JsonProperty("events")>
    Public Property Events As Boolean = False

    <JsonProperty("moduleRenaming")>
    Public Property ModuleRenaming As Boolean = False

    <JsonProperty("moduleInvisible")>
    Public Property ModuleInvisible As Boolean = False

    <JsonProperty("unsafeMode")>
    Public Property UnsafeMode As Boolean = False

    <JsonProperty("resourceEncryption")>
    Public Property ResourceEncryption As Boolean = False

    <JsonProperty("resourceCompressEncrypt")>
    Public Property ResourceCompressEncrypt As Boolean = False

End Class

Public Class ProtectionSettings

    <JsonProperty("importProtection")>
    Public Property ImportProtection As Boolean = False

    <JsonProperty("sufConfusion")>
    Public Property SUFConfusion As Boolean = False

    <JsonProperty("proxyClasses")>
    Public Property ProxyClasses As Boolean = False

    <JsonProperty("proxyClassesUnsafe")>
    Public Property ProxyClassesUnsafe As Boolean = False

    <JsonProperty("proxyMethods")>
    Public Property ProxyMethods As Boolean = False

    <JsonProperty("moveVariables")>
    Public Property MoveVariables As Boolean = False

    <JsonProperty("l2f")>
    Public Property L2F As Boolean = False

    <JsonProperty("entryPointMover")>
    Public Property EntryPointMover As Boolean = False

    <JsonProperty("method2Delegate")>
    Public Property Method2Delegate As Boolean = False

    <JsonProperty("method2DynamicEntry")>
    Public Property Method2DynamicEntry As Boolean = False

    <JsonProperty("reduceMetadata")>
    Public Property ReduceMetadata As Boolean = False

    <JsonProperty("nopAttack")>
    Public Property NopAttack As Boolean = False

    <JsonProperty("stringEncryption")>
    Public Property StringEncryption As Boolean = False

    <JsonProperty("proxyStrings")>
    Public Property ProxyStrings As Boolean = False

    <JsonProperty("dynamicStrings")>
    Public Property DynamicStrings As Boolean = False

    <JsonProperty("replaceObfuscation")>
    Public Property ReplaceObfuscation As Boolean = False

    <JsonProperty("intConfusion")>
    Public Property IntConfusion As Boolean = False

    <JsonProperty("stringsHider")>
    Public Property StringsHider As Boolean = False

    <JsonProperty("calli")>
    Public Property Calli As Boolean = False

    <JsonProperty("proxyInt")>
    Public Property ProxyInt As Boolean = False

    <JsonProperty("dynamicInt")>
    Public Property DynamicInt As Boolean = False

    <JsonProperty("mutationV2")>
    Public Property MutationV2 As Boolean = False

    <JsonProperty("mutationV2Unsafe")>
    Public Property MutationV2Unsafe As Boolean = False

    <JsonProperty("encodeIntegers")>
    Public Property EncodeIntegers As Boolean = False

    <JsonProperty("fakeObfuscation")>
    Public Property FakeObfuscation As Boolean = False

    <JsonProperty("addJunkCode")>
    Public Property AddJunkCode As Boolean = False

    <JsonProperty("junkCodeNumber")>
    Public Property JunkCodeNumber As Decimal = 10

    <JsonProperty("controlFlow")>
    Public Property ControlFlow As Boolean = False

    <JsonProperty("controlFlowStrong")>
    Public Property ControlFlowStrong As Boolean = False

    <JsonProperty("sugarControlFlow")>
    Public Property SugarControlFlow As Boolean = False

    <JsonProperty("KroksControlFlow")>
    Public Property KroksControlFlow As Boolean = False

    <JsonProperty("exGuardControlFlow")>
    Public Property EXGuardControlFlow As Boolean = False

    <JsonProperty("proxyReferences")>
    Public Property ProxyReferences As Boolean = False

    <JsonProperty("proxyReferencesUnsafe")>
    Public Property ProxyReferencesUnsafe As Boolean = False

    <JsonProperty("mutator")>
    Public Property Mutator As Boolean = False

    <JsonProperty("mutatorUnsafe")>
    Public Property MutatorUnsafe As Boolean = False

    <JsonProperty("antiDecompiler")>
    Public Property AntiDecompiler As Boolean = False

    <JsonProperty("invalidOpcodes")>
    Public Property InvalidOpcodes As Boolean = False

    <JsonProperty("invalidMD")>
    Public Property InvalidMD As Boolean = False

    <JsonProperty("stackUnfConfusion")>
    Public Property StackUnfConfusion As Boolean = False

    <JsonProperty("hideMethods")>
    Public Property HideMethods As Boolean = False

    <JsonProperty("dynamicMethods")>
    Public Property DynamicMethods As Boolean = False

    <JsonProperty("cctorHider")>
    Public Property CctorHider As Boolean = False

    <JsonProperty("dynamicCctor")>
    Public Property DynamicCctor As Boolean = False

    <JsonProperty("cctorL2F")>
    Public Property CctorL2F As Boolean = False

    <JsonProperty("methodError")>
    Public Property MethodError As Boolean = False

    <JsonProperty("codeOptimizer")>
    Public Property CodeOptimizer As Boolean = False

End Class

Public Class PackerSettings

    <JsonProperty("usePacker")>
    Public Property UsePacker As Boolean = False

    <JsonProperty("selectedPacker")>
    Public Property SelectedPacker As Integer = 0

End Class

Public Class OutputSettings

    <JsonProperty("preserveAll")>
    Public Property PreserveAll As Boolean = False

    <JsonProperty("invalidMetadata")>
    Public Property InvalidMetadata As Boolean = False

    <JsonProperty("unmanagedString")>
    Public Property UnmanagedString As Boolean = False

    <JsonProperty("exportEntryPoint")>
    Public Property ExportEntryPoint As Boolean = False

    <JsonProperty("peSectionPreserve")>
    Public Property PESectionPreserve As Boolean = False

    <JsonProperty("peSectionCustom")>
    Public Property PESectionCustom As Boolean = False

    <JsonProperty("peSectionCustomText")>
    Public Property PESectionCustomText As String = ""

    <JsonProperty("peSectionExclusion")>
    Public Property PESectionExclusion As String = ""

    <JsonProperty("jitHook")>
    Public Property JITHook As Boolean = False

    <JsonProperty("signPE")>
    Public Property SignPE As Boolean = False

    <JsonProperty("certMode")>
    Public Property CertMode As Integer = 0

    <JsonProperty("certPath")>
    Public Property CertPath As String = ""

    <JsonProperty("appClosesMethod")>
    Public Property AppClosesMethod As Integer = 0

End Class

Public Class VMSettings

    <JsonProperty("enabled")>
    Public Property Enabled As Boolean = False

    <JsonProperty("selectedVM")>
    Public Property SelectedVM As Integer = 0

    <JsonProperty("protectRuntime")>
    Public Property ProtectRuntime As Boolean = False

    <JsonProperty("virtualizeStrings")>
    Public Property VirtualizeStrings As Boolean = False

    <JsonProperty("selectAll")>
    Public Property SelectAll As Boolean = False

    <JsonProperty("excludeConstructors")>
    Public Property ExcludeConstructors As Boolean = False

    <JsonProperty("excludeRedMethods")>
    Public Property ExcludeRedMethods As Boolean = False

    <JsonProperty("excludeUnsafeMethods")>
    Public Property ExcludeUnsafeMethods As Boolean = False

End Class

Public Class AntiSettings

    <JsonProperty("antiProxy")>
    Public Property AntiProxy As Boolean = False

    <JsonProperty("exceptionManager")>
    Public Property ExceptionManager As Boolean = False

    <JsonProperty("elevationEscale")>
    Public Property ElevationEscale As Boolean = False

    <JsonProperty("antiDebug")>
    Public Property AntiDebug As Boolean = False

    <JsonProperty("jitFucker")>
    Public Property JitFucker As Boolean = False

    <JsonProperty("antiDump")>
    Public Property AntiDump As Boolean = False

    <JsonProperty("protectAntiDump")>
    Public Property ProtectAntiDump As Boolean = False

    <JsonProperty("extremeAD")>
    Public Property ExtremeAD As Boolean = False

    <JsonProperty("antiHTTPDebug")>
    Public Property AntiHTTPDebug As Boolean = False

    <JsonProperty("antiInvoke")>
    Public Property AntiInvoke As Boolean = False

    <JsonProperty("antiTamper")>
    Public Property AntiTamper As Boolean = False

    <JsonProperty("antide4dot")>
    Public Property Antide4dot As Boolean = False

    <JsonProperty("antiMalicious")>
    Public Property AntiMalicious As Boolean = False

    <JsonProperty("antiILDasm")>
    Public Property AntiILDasm As Boolean = False

    <JsonProperty("antiAttach")>
    Public Property AntiAttach As Boolean = False

    <JsonProperty("threadHider")>
    Public Property ThreadHider As Boolean = False

    <JsonProperty("bypassAmsi")>
    Public Property BypassAmsi As Boolean = False

End Class

Public Class DLLSettings

    <JsonProperty("dllEmbeder")>
    Public Property DLLEmbeder As Boolean = False

    <JsonProperty("selectAllDlls")>
    Public Property SelectAllDlls As Boolean = False

    <JsonProperty("mergeMode")>
    Public Property MergeMode As Integer = 0

End Class

Public Class EntryPointSettings

    <JsonProperty("hasCustomEntryPoint")>
    Public Property HasCustomEntryPoint As Boolean = False

    <JsonProperty("entryPointToken")>
    Public Property EntryPointToken As UInteger = 0

    <JsonProperty("entryPointMethodName")>
    Public Property EntryPointMethodName As String = ""

    <JsonProperty("entryPointTypeName")>
    Public Property EntryPointTypeName As String = ""

    <JsonProperty("assemblyName")>
    Public Property AssemblyName As String = ""

    <JsonProperty("assemblyFullName")>
    Public Property AssemblyFullName As String = ""

End Class