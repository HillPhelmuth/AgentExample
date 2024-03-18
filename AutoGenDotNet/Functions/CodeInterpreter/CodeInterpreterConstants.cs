namespace AutoGenDotNet.Functions.CodeInterpreter;

public class CodeInterpreterConstants
{
    public const string InputDescription =
        """
        The python code to execute.
        Make sure you follow the correct Python code syntax before submitting it.

        Do not add requirement installations here; those requirements are supposed to be in ``requirements`` input parameters.

        If you expect me to send you a result, you should use ``print`` method to output your expectactions.

        ## Example
        ```
        x = 1
        y = 2.8
        z = 1j

        print(type(x))
        print(type(y))
        print(type(z))
        ```

        This code should be sended like this:
        ```
        x = 1\r\ny = 2.8\r\nz = 1j\r\n\r\nprint(type(x))\r\nprint(type(y))\r\nprint(type(z))
        ```
        """;
    public const string RequirementsDescription =
        """
        The contents of the Python requirements file to be used.
        These requirements will be added to the ``requirements.txt`` file in the sandbox by the CodeInterpreter.

        ## Example
        ```
        tensorflow
        uvicorn
        fastapi==0.63.0
        ```
        """;
    public const string BindingsDescription =
        """
        The list of input files to bind to the code.
        These files will be linked to the `/var/app/inputs/`` directory in the sandbox. So, when they are linked, they should be used directly in ``/var/app/inputs/``, omitting their relative path.
        This list should be provided as a json document, and each item should be represented as a bind string.

        ## Example
        ```json
        [
              "<Full_File_Path>:/var/app/inputs/<File name>:ro",
              "/home/user/myFile.xls:/var/app/inputs/myFile.xls:rw"
        ]
        ```
        """;
    public const string CombinedDescription =
        """
        # Code input
        The python code to execute.
        Make sure you follow the correct Python code syntax before submitting it.

        Do not add requirement installations here; those requirements are supposed to be in ``requirements`` input parameters.

        If you expect me to send you a result, you should use ``print`` method to output your expectactions.

        ## Example
        ```
        x = 1
        y = 2.8
        z = 1j

        print(type(x))
        print(type(y))
        print(type(z))
        ```

        This code should be sended like this:
        ```
        x = 1\r\ny = 2.8\r\nz = 1j\r\n\r\nprint(type(x))\r\nprint(type(y))\r\nprint(type(z))
        ```
        # Requirements input
        The contents of the Python requirements file to be used.
        These requirements will be added to the ``requirements.txt`` file in the sandbox by the CodeInterpreter.

        ## Example
        ```
        tensorflow
        uvicorn
        fastapi==0.63.0
        ```
        # Bindings Input
        The list of input files to bind to the code.
        These files will be linked to the `/var/app/inputs/`` directory in the sandbox. So, when they are linked, they should be used directly in ``/var/app/inputs/``, omitting their relative path.
        This list should be provided as a json document, and each item should be represented as a bind string.

        ## Example
        ```json
        [
              "<Full_File_Path>:/var/app/inputs/<File name>:ro",
              "/home/user/myFile.xls:/var/app/inputs/myFile.xls:rw"
        ]
        ```
        """;
}