[<AutoOpen>]
module Messages

type ProcessCommand = 
| ContinueProcessing

type ErrorType =
| Null
| Validation

type InputResult =
| InputSuccess of string
| InputError of reason: string * errorType: ErrorType