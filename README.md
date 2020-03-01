# PI_Projects
PI Application Developer Accreditation

A .Net console utility  which provides 2 options for backfilling - 1. Manually backfill for selected period 2. Automatically backfill the result against input attribute’s OOO event (based on AFdatapipe).
User can input AF server name, AF DB name, Element path of choice and then select a particular analysis for processing.
Language: VB.NET


Changes / additions in the code: 
•         Handled null exceptions for the declared objects and added better error handing for invalid entries for objects such as AF server, AF DB, AF Element, Start time, End Time. 
•         More meaningful exception messages are printed. 
•         I have removed attributes signups for AF DataPipe and disposed objects on completion of the iobserver operation and when the exception/application crash is encountered post attributes sign-ups. 
 
