/* librerías que usaremos */
#include <mysql/mysql.h> /* libreria que nos permite hacer el uso de las conexiones y consultas con MySQL */
#include <stdio.h> /* Para poder usar printf, etc. */
#include <string.h>
#include <stdlib.h>
#include <time.h>
#include <wiringPi.h>
#include <softTone.h>
#include <pitches.h>
#define PIN 3

void lanzaQuery(MYSQL *conn, char* query, MYSQL_RES *res);
int existeRegistro(MYSQL *conn, char* queryFinalizado, MYSQL_RES *res, MYSQL_ROW row, char *valor, int resultadoNecesario);
void enciendeDosLeds(int numVeces);
void enciendeLed(int color);
void parpadeaLed(int color, int numVeces);

int scale [] = { NOTE_E7, NOTE_E7, 0, NOTE_E7, 
  0, NOTE_C7, NOTE_E7, 0,
  NOTE_G7, 0, 0,  0,
  NOTE_G6, 0, 0, 0, } ;

main() {
	MYSQL *conn;
	MYSQL_RES *res = NULL;
	MYSQL_ROW row;
	/* datos para conectarse a la base de datos */
	char *server = "YOURADDRESS";
	char *user = "YOURUSER";
	char *password = "YOURPASSWORD"; /* set me first */
	char *database = "YOURDATABASE";

	/* Sentencia SQL que le vamos a mandar y variables donde guardar la sentencia */
	char *sql = "UPDATE sat SET avisado='Si',donde='Recogidas', notasRMA=" ;
	char *sql01 ="'\r\n' Cliente avisado con fecha ";
	char *comillas = "'";
 	char *sql1="' WHERE idSat=";
 	char *sql2=" AND finalizado='Si'";
 	char *sqlAlt  ="SELECT notasRMA FROM sat WHERE idSat=";
 	char *saltoDeLinea = "\n";
 	char querySQLalt[100];
 	char numSat[10];
 	char querySQL[100];
 	char concatenasql[100000];
 	char estaFinalizado[10];
 	char concatenanotas[100000];
 	char queryFinalizado[1000];
 	char *sqlFinalizado ="SELECT finalizado FROM sat WHERE idSat=";
	/* Modulos GPIO / buzzer */
 	int i;
	wiringPiSetup();
	softToneCreate (PIN) ;
	pinMode (0, OUTPUT);
 	pinMode (1, OUTPUT);
	pinMode (2, OUTPUT);
	delay (5000);
	/* Musica inicial */
	for(i=0;i<16;i++){
  		softToneWrite (PIN, scale [i]) ;
  		delay (60) ;
  		softToneWrite (PIN, scale [i]) ;
  		delay(60);
   	}
 
	softToneWrite (PIN,0);
	/* Empezando el programa */
	while(1){
		/* Iniciamos el led azul (en espera) */
		digitalWrite(2,HIGH);
		/* Preparamos la conexion de mysql */
        	conn = mysql_init(NULL);
		/* Recogemos el n. de SAT por codigo de barras o teclado si es diferente de letras y entre 0 y 100000*/
		do{	
			printf("Introduce el SAT para marcar como avisado (q para salir(\n");
			scanf("%s",numSat);   
			if(strcmp(numSat,"q") == 0 || strcmp(numSat,"Q")  == 0){
				printf("Has introducido q, nos salimos\n");
				digitalWrite(2,LOW);
				exit(1);
			}
		}while(atoi(numSat) <1 || atoi(numSat) > 100000);
		/* Establecemos la variable de tiempo local */
		time_t tiempo = time(0);
		char output[128];
		struct tm *tlocal = localtime(&tiempo);
       		int numRegistros = 0;
		/* Metemos la hora para saber cuando se paso */
		strftime(output,128, "%Y/%m/%d %H:%M:%S",tlocal);
		/* Connect to database */
		if (!mysql_real_connect(conn, server,
        	 user, password, database, 0, NULL, 0)) {
      			fprintf(stderr, "%s\n", mysql_error(conn));
      			digitalWrite(2,LOW);
      			exit(1);
   		}
		/* Establecemos la query para ver si esta ya finaliza */
		strcpy(queryFinalizado,"");
		strcat(queryFinalizado,sqlFinalizado);
		strcat(queryFinalizado,numSat);
		printf("queryFinalizado vale %s",queryFinalizado);
	
/* Si esta finalizado lo marcaremos como avisado */
		if(existeRegistro(conn,queryFinalizado,res,row,"Si",0))
		{
			char sqltemporal[1000];
			strcpy (sqltemporal,"");
			strcat (sqltemporal, "SELECT avisado FROM sat WHERE idSat=");
			strcat (sqltemporal, numSat);
			printf ("sql temporal vale: %s\n",sqltemporal);
			/* Si esta finalizado y ademas esta avisado el cliente metemos registro en segsat */
			if(existeRegistro(conn,sqltemporal,res,row,"Si",0))
			{
				strcpy (sqltemporal,"");
				strcat (sqltemporal,"INSERT INTO segSat(idSat, Accion, descrAccion, fecha, hora, usuario, estado, notas) 					VALUES ('");
				strcat(sqltemporal,numSat);
				strcat(sqltemporal,"', 'Llamada', 'Cliente Avisado por telefono', '");
				strcat(sqltemporal, output);
				strcat(sqltemporal, "','");
				strcat(sqltemporal, output);
				strcat(sqltemporal,"', 'AUTOMATIZADO', 'Completado', 'R2Pi')");
				printf ("segSat Lanzado: %s",sqltemporal);
				lanzaQuery(conn,sqltemporal, res);
				res = mysql_use_result(conn);
				enciendeDosLeds(1);
			}
			/* Si no esta avisado pero si finalizado, marcamos como cliente avisado y ubicacion en Recogidas */
			else{
				/* Metemos la llamada */				
				strcpy (sqltemporal,"");
				strcat (sqltemporal,"INSERT INTO segSat(idSat, Accion, descrAccion, fecha, hora, usuario, estado, notas) 					VALUES ('");
				strcat(sqltemporal,numSat);
				strcat(sqltemporal,"', 'Llamada', 'Cliente Avisado por telefono', '");
				strcat(sqltemporal, output);
				strcat(sqltemporal, "','");
				strcat(sqltemporal, output);
				strcat(sqltemporal,"', 'AUTOMATIZADO', 'Completado', 'R2Pi')");
				printf ("segSat Lanzado: %s\n",sqltemporal);
				lanzaQuery(conn,sqltemporal, res);
				/* Modificamos el registro como cliente avisado y su nueva ubicacion */				
				strcpy (sqltemporal,"");
				strcat (sqltemporal,"UPDATE sat SET donde='Recogidas', avisado='Si' WHERE idSat='");
				strcat(sqltemporal,numSat);
				strcat(sqltemporal,"'");
				printf ("segSat Lanzado: %s\n",sqltemporal);
				lanzaQuery(conn,sqltemporal, res);
				enciendeLed(0);
			}
 		}
		/*Si esta sin finalizar parpadea */
		else{
			/* Parpadea 5 veces rapidas el led rojo */
			softToneWrite (PIN, 500) ;			
			parpadeaLed(1,5);
			printf("\nEste id esta sin finalizar\n");
			softToneWrite (PIN,0);
		} 
   	/* Libero donde se guarda el resultado */
   	mysql_free_result(res);
   	/* close connection */
   	mysql_close(conn);
	} 
}

void lanzaQuery(MYSQL *conn, char* query, MYSQL_RES *res){
	printf("Lanzando query %s:\n",query);	
	if (mysql_query(conn, query)) {
      		fprintf(stderr, "%s\n", mysql_error(conn));
      		digitalWrite(2,LOW);
      		exit(1);
   	}
}

int existeRegistro(MYSQL *conn, char* queryFinalizado, MYSQL_RES *res, MYSQL_ROW row, char *valor, int resultadoNecesario){
/* Lanzamos la query para extraer si esta avisado */
	char estaFinalizado[10];
	if (mysql_query(conn, queryFinalizado)) {
      		fprintf(stderr, "%s\n", mysql_error(conn));
      		digitalWrite(2,LOW);
      		exit(1);
   	}
	/* Guardamos el resultasdo en res */
   	res = mysql_use_result(conn);
	strcpy (estaFinalizado,""); 
	while(row  = mysql_fetch_row(res)){
		strcat (estaFinalizado,row[0]); 
	}
	/*printf("\nestaFinalizado vale: %s\n",estaFinalizado);*/
	/* Liberamos la query */
	/*printf("\nstrcmp da un resultado de %d",strcmp(estaFinalizado,valor));*/
	/* Si esta finalizado lo marcaremos como avisado */
	/* strcmp devuelve un 0 si las 2 cadenas son identicas y un <0 o >0 si son diferentes */
	if(strcmp(estaFinalizado,valor) == resultadoNecesario){
		printf("Voy a devolver un 1");
		return 1;
	}
	else{
		printf("Voy a devolver un 0");
		return 0;
	}
}

void enciendeDosLeds(int numVeces){
	int x;
	for(x=0;x<numVeces;x++){
		digitalWrite(1,HIGH);
		digitalWrite(0,HIGH);
		delay(650);
		digitalWrite(1,LOW);
		digitalWrite(0,LOW);
		delay (650);
	}
}

/*0 verde - 1 rojo */
void enciendeLed(int color){
	digitalWrite(color,HIGH);
	delay(500);
	digitalWrite(color,LOW);
}
void parpadeaLed(int color, int numVeces){
	/* Parpadea 5 veces rapidas el led rojo */
	int x;
	for(x=0;x<numVeces;x++){
		digitalWrite(color,HIGH);
		delay(100);
		digitalWrite(color,LOW);
		delay(100);
	}
} 
