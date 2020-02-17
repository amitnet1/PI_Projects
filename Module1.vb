'Developed by: Amit Patil
'Date Created: 30/01/2020   Date Modified: 17/02/2020 (Added additional error handling & disposed datapipe objects on completion & on error)

'Program for testing AF Analysis backfilling / reclaulation 
'Target feamework shall be 4.6.2 Or later for AF (Include OSIsoft.AFSDK version 4.0.0.0 under references)
'Imports OSIsoft.AF.PI   'Namespace required for AF SDK PI Data Archive Server
Imports OSIsoft.AF       'Namespace required for AFErrors, PIsystems(AF)
Imports OSIsoft.AF.Asset 'Namespace required for AF SDK PI AF values, attributes
Imports OSIsoft.AF.Time  'Namespace required for AF SDK PI timerange, span
Imports OSIsoft.AF.Analysis 'Namespace required for AF Analysis
Imports OSIsoft.AF.Data 'Name spaces for AF data events
Module Module1
    'Global variables
    'Dim g_PIServers As OSIsoft.AF.PI.PIServers  'Global declaration of PI servers
    Dim g_AFServers As OSIsoft.AF.PISystems     'Global declaration of AF servers
    Dim myAFServer As PISystem  'AF Server
    Dim myAFDatabases As AFDatabases 'AF DB Collection
    Dim myAFDatabase As AFDatabase  'AF Database of interest
    Dim myAFElement As AFElement    'Target Element
    Public Pipe_Val_Action As String
    Public Pipe_Val_Timestamp As String

    Sub Main()
        Console.Title = "PI Analysis reclaulation test utility (AF SDK) - AP"
Selection:

        Console.BackgroundColor = ConsoleColor.Black
        Console.ForegroundColor = ConsoleColor.White
        Console.WriteLine() 'Blank line

        Call Analysis_Recalc()

        Console.Write("Do you want to continue? Y/N: ")
        Dim YNselection As String = Console.ReadLine()
        If YNselection = "Y" Or YNselection = "y" Then
            GoTo Selection
        Else
            Console.WriteLine("Thank you! Exiting the program...")
        End If

    End Sub
    Public Sub Analysis_Recalc()
        Dim temp_afsrv, temp_af_db, temp_af_element As String
        Dim temp_selection As Int16
        Dim timeRange As AFTimeRange
        Dim _afDataPipe As AFDataPipe
        Dim iobserver2 As AFConsoleDataReceiver
        Dim mode As AFAnalysisService.CalculationMode
        Dim AN_Service As AFAnalysisService
        Dim analyses As IEnumerable(Of AFAnalysis)
        Dim returnValue As Object
        Dim pipe_attributelist As AFAttributeList

        Try

            g_AFServers = New PISystems 'Instantiate a new AF servers object
            Console.Write("Enter AF Server name to be connected: ")
            temp_afsrv = Console.ReadLine()
            'temp_afsrv = "AZSG-D-DSPI"
            myAFServer = g_AFServers.Item(temp_afsrv)
            myAFServer.Connect()
            myAFDatabases = myAFServer.Databases
            Console.WriteLine() 'Blank line
            Console.Write("Enter AF DB: ")
            temp_af_db = Console.ReadLine()
            'temp_af_db = "Lab Analysis"
            myAFDatabase = myAFDatabases(temp_af_db)
            Console.WriteLine("AF Server name: " & myAFServer.Name & ", AF DB:" & myAFDatabase.Name)
            Console.Write("Enter target AF Element name: ") 'Path: Parent\Child format
            temp_af_element = Console.ReadLine()
            myAFElement = myAFDatabase.Elements(temp_af_element)
            Console.WriteLine() 'Blank line

            'check for analyses against selected element
            If myAFElement.Analyses Is Nothing Then
                Console.WriteLine("No analyses available for the selected element " & myAFElement.Name & ".")
                GoTo Disconnect_AF
            End If

            Console.WriteLine("Available Analyses are as follows:")
            For i = 0 To myAFElement.Analyses.Count - 1
                Dim tempstring As String = "" & vbTab & i + 1 & "." & vbTab & myAFElement.Analyses.Item(i).ToString
                Console.WriteLine(tempstring)
            Next

            Console.WriteLine() 'Blank line
            Console.Write("Select analysis number for recalculation: ")
            Dim calc_number As Int16 = Convert.ToInt16(Console.ReadLine()) - 1  'offset adjusted
            Dim analysis1 As AFAnalysis
            analysis1 = myAFElement.Analyses.Item(calc_number)

            Console.WriteLine() 'Blank line
            Console.WriteLine("Select option number.")
            Console.Write("1:Manual backfilling 2.Auto Trigegred backfilling (based on AF data-pipe monitoring OOO event): ")
            temp_selection = Convert.ToInt16(Console.ReadLine())

            AN_Service = myAFServer.AnalysisService
            analyses = {analysis1}  'Initilize analysis collection


            Select Case temp_selection
                Case 1
                    'Manual backfilling
                    Dim start_time, end_time As String
                    Console.WriteLine("Option 1 - Manual backfilling has been selected.")
                    Console.Write("Select start time for recalc:")
                    start_time = Console.ReadLine()
                    Console.Write("Select end time for recalc:")
                    end_time = Console.ReadLine()
                    timeRange = AFTimeRange.Parse(start_time, end_time, myAFServer.ServerTime, Nothing)
                    mode = AFAnalysisService.CalculationMode.FillDataGaps 'Backfill manually
                    returnValue = AN_Service.QueueCalculation(analyses, timeRange, mode)
                    Console.WriteLine("Manual recalculation scheduled for " & "Start time-" &
                                      timeRange.StartTime.ToString & "End time-" & timeRange.EndTime.ToString &
                                      "on " & AN_Service.Host)

                Case 2
                    'AF Datapipe based backfilling of out of order event
                    Console.WriteLine("Option 2 - Auto Trigegred backfilling has been selected (OOO processing based on data pipe).")
                    Console.Write("Enter the time in seconds for which data pipe events shall be monitored: ")
                    Dim datapipetimer As Int16 = Convert.ToInt16(Console.ReadLine())

                    _afDataPipe = New AFDataPipe
                    'Dim instance As AFAnalysisRuleConfiguration 'Not required
                    'instance = analysis1.AnalysisRule.GetConfiguration 'Not required

                    pipe_attributelist = analysis1.AnalysisRule.GetInputs
                    _afDataPipe.AddSignups(pipe_attributelist)
                    iobserver2 = New AFConsoleDataReceiver
                    _afDataPipe.Subscribe(iobserver2)   'Subscribe and pass iobserver reference
                    Console.WriteLine() 'Blank line
                    'Use Get Observer events 
                    Dim i As Int16
                    While i <= (datapipetimer - 1)
                        'Console.WriteLine(i)
                        _afDataPipe.GetObserverEvents() 'Get AFdatapipe events then implement further logic
                        'Capture OOO event and perform recalculation for single new OOO event automatically (inserted or modified)
                        If Pipe_Val_Action = "Update" Then
                            timeRange = AFTimeRange.Parse(Pipe_Val_Timestamp, Pipe_Val_Timestamp, myAFServer.ServerTime, Nothing)
                            mode = AFAnalysisService.CalculationMode.FillDataGaps   'Backfill automatically for single OOO timestamp
                            returnValue = AN_Service.QueueCalculation(analyses, timeRange, mode)
                            Console.ForegroundColor = ConsoleColor.Red  'Red font
                            Console.WriteLine("OOO event detected for input attribute.")
                            Console.WriteLine("Automatic backfilling triggered for time: " & Pipe_Val_Timestamp)
                        ElseIf Pipe_Val_Action = "Add" Or Pipe_Val_Action = "Delete" Then
                            Console.ForegroundColor = ConsoleColor.Red  'Red font
                            Console.WriteLine("This is not an OOO update event. No automatic backfilling action required.")
                        End If
                        Console.ForegroundColor = ConsoleColor.White    'Reset font color to original
                        Pipe_Val_Action = Nothing 'clear
                        Pipe_Val_Timestamp = Nothing 'clear
                        Threading.Thread.Sleep(1000)    'Delay in ms , loop will exit after no. of seconds captured by datapipetimer
                        i = i + 1
                    End While

                    'Dispose datapipe related objects on completion of iobserver loop to release resource overheads
                    If (pipe_attributelist IsNot Nothing Or _afDataPipe IsNot Nothing) And (pipe_attributelist.Count > 0) Then
                        _afDataPipe.RemoveSignups(pipe_attributelist) ' Remove signups on complete
                        _afDataPipe.Dispose()   'Dispose data pipe on complete
                        Console.WriteLine("Data event (OOO) Observer time completed. Time: " & datapipetimer & " seconds.")
                        Console.WriteLine("AF DataPipe related objects disposed.")
                    End If
                Case Else
                    Console.WriteLine("Invalid option selected for backfilling! Exiting the program..")
            End Select
Disconnect_AF:
            myAFServer.Disconnect() 'Disconnect after completion
            Console.WriteLine("**********************************************")

        Catch ex As Exception
            Console.ForegroundColor = ConsoleColor.Red
            If myAFServer Is Nothing Then
                Console.WriteLine("Invalid AF Server Name: " & temp_afsrv & ".")
                Console.WriteLine("Kindly enter valid AF Server name.")
            ElseIf myAFDatabase Is Nothing Then
                Console.WriteLine("Invalid AF DB Name: " & temp_af_db & ".")
                Console.WriteLine("Kindly enter valid AF DB name.")
                myAFServer.Disconnect() 'Disconnect on exception
            ElseIf myAFElement Is Nothing Then
                Console.WriteLine("Invalid AF Element Path: " & temp_af_element & ".")
                Console.WriteLine("Kindly specify valid AF Element path (parent\child element ref. format).")
                myAFServer.Disconnect() 'Disconnect on exception
            ElseIf timeRange.StartTime.LocalTime.Year = "1970" Or timeRange.EndTime.LocalTime.Year = "1970" Then
                Console.WriteLine("Error while parsing Start Time or End Time due to invalid entry.")
                Console.WriteLine("Please enter Start Time or End Time in Windows clock format or PI format.")
            Else
                Console.WriteLine("Exception: " & ex.Message)
                Console.WriteLine("Kindly contact AF system admin if the issue persists.")
                myAFServer.Disconnect()
            End If

            'Dispose datapipe related objects on exception
            If (pipe_attributelist IsNot Nothing And _afDataPipe IsNot Nothing And pipe_attributelist IsNot Nothing) Then
                _afDataPipe.RemoveSignups(pipe_attributelist) ' Remove signups on error
                _afDataPipe.Dispose()   'Dispose data pipe on error
                myAFServer.Disconnect() 'Disconnect on exception
                g_AFServers.DisconnectAll()
            End If

            Console.WriteLine("**********************************************")
            'Reset theme on completion of exception capturing
            Console.BackgroundColor = ConsoleColor.Black
            Console.ForegroundColor = ConsoleColor.White
            Console.WriteLine() 'Blank line
        End Try
    End Sub

End Module

Public Class AFConsoleDataReceiver

    'Derived class of iObserver for AF Data Pipe
    'Refer - How to use the PIDataPipe or the AFDataPipe (PI Square ref)
    Implements IObserver(Of AFDataPipeEvent)

    Public Sub OnNext(value As AFDataPipeEvent) Implements IObserver(Of AFDataPipeEvent).OnNext
        Console.WriteLine("AFDataPipe event - Attribute Name: {0}, Action Type: {1}, Value {2}, TimeStamp: {3}", value.Value.Attribute.Name, value.Action.ToString(), value.Value.Value, value.Value.Timestamp.ToString())
        Pipe_Val_Action = value.Action.ToString()   'Public var
        Pipe_Val_Timestamp = value.Value.Timestamp.ToString 'Public var
        'Console.WriteLine("Action:" & Pipe_Val_Action & ", Timestamp: " & Pipe_Val_Timestamp)
    End Sub

    Public Sub OnError([error] As Exception) Implements IObserver(Of AFDataPipeEvent).OnError
        Console.WriteLine("Provider has sent an error.")

    End Sub

    Public Sub OnCompleted() Implements IObserver(Of AFDataPipeEvent).OnCompleted
        Console.WriteLine("Provider has completed/terminated sending data.")
    End Sub

End Class
