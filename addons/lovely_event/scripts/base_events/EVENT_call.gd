extends EVENT
## Calls given function with optional given variables.
class_name EVENT_call

var call_function : Callable;
var call_vars : Array = [];

## calls [param function] with given [param variables] on execute.
func _init( function : Callable, ...variables ) -> void:
	call_function = function;
	for v in variables:
		call_vars.append( v );
	is_skippable = true;
	super._init();


func execute( _looping : bool, _dt : float ) -> RETURNTYPE:
	if not call_vars.is_empty():
		call_function.callv( call_vars );
	else:
		call_function.call();
	return RETURNTYPE.FINISHED;


func on_skip() -> void:
	if not call_vars.is_empty():
		call_function.callv( call_vars );
	else:
		call_function.call();
