grammar Lang;

program	:	functionDeclaration+;

functionDeclaration
	:	(PROCKW | FUNCKW) NAME LBRACK NAME? (COMMA NAME)* RBRACK
		LCURLY
			variableDeclaration?
			block
		RCURLY
	;

variableDeclaration	:	VARKW NAME (COMMA NAME)* SEMICOLON;

block	:	statement*;

statement
	:	expression SEMICOLON
	|	returnStatement
	|	setStatement
	|	ifStatement
	;

returnStatement	:	RETURNKW expression? SEMICOLON;
setStatement	:	NAME SET expression SEMICOLON;
ifStatement	:	IFKW LBRACK expression RBRACK LCURLY block RCURLY (ELSEKW LCURLY block RCURLY)?;

expression
	:	NAME LBRACK expression? (COMMA expression)* RBRACK
	|	expression MULPRIOR expression
	|	expression (ADD | SUB) expression
	|	expression (RELATIONPRIOR) expression
	|	expression (OPAND) expression
	|	expression (OPOR) expression
	|	(ADD | SUB)? LBRACK expression RBRACK
	|	(ADD | SUB)? atom
	;

atom
	:	NUMBER
	|	NAME
	;

SEMICOLON	:	';';
COMMA	:	',';

FUNCKW	:	'func';
PROCKW	:	'proc';
RETURNKW	:	'return';
IFKW	:	'if';
ELSEKW	:	'else';
VARKW	:	'var';

NAME	:	NAMEPREF NAMEPOST*;
NUMBER	:	[0-9]+;

MULPRIOR
	:	MUL
	|	DIV
	;

// should be 2 groups
RELATIONPRIOR
	:	ROL
	|	ROG
	|	ROE
	|	RONE
	;

OPAND	:	'&&';
OPOR	:	'||';

ADD	:	'+';
SUB	:	'-';
SET	:	'=';
fragment MUL	:	'*';
fragment DIV	:	'/';

fragment ROL	:	'<';
fragment ROG	:	'>';
fragment ROE	:	'==';
fragment RONE	:	'!=';

LBRACK	:	'(';
RBRACK	:	')';

LCURLY	:	'{';
RCURLY	:	'}';

fragment NAMEPREF	:	[a-zA-Z_]; // [\p{L}]
fragment NAMEPOST	:	NAMEPREF | [0-9];

LINE_COMMENT
	:	'//' ~[\r\n]*
	->	skip
	;

WS
	: [ \t\n\r]+
	-> skip
	;
