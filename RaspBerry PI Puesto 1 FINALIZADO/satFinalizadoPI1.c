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
char ultimoNumSatCogido[10];

main() {
	MYSQL *conn;
	MYSQL_RES *res = NULL;
	MYSQL_ROW row;
	char *server = "YOURIPADDRESS";
   	char *user = "YOURUSER";
   	char *password = "YOURPASSWORD"; /* set me first */
   	char *database = "YOURDATABASE";
   	char *sqlNoReparado = "UPDATE sat SET finalizado='Si', donde='Mesa Roja',diagnostico='No Reparable',finSat='";

	/*   char *sqlTiempoDesdeQueMeti= "SELECT finSat */
   	char numSat[10];
   	char querySQL[1000];
   	strcpy(ultimoNumSatCogido,"");
   	int i;
  	wiringPiSetup();
   	softToneCreate (PIN) ;
   	pinMode (0, OUTPUT); //Led Verde
   	pinMode (1, OUTPUT); //Led Rojo
   	pinMode (2, OUTPUT); //Led Azul
	/* Musica inicial */
  	for(i=0;i<16;i++){
  		softToneWrite (PIN, scale [i]) ;
  		delay (60) ;
  		softToneWrite (PIN, scale [i]) ;
  		delay(60);
   	}
   	softToneWrite (PIN,0);


	while(1){
		/* Iniciamos el led azul (en espera) */
		digitalWrite(2,HIGH);
        	conn = mysql_init(NULL);
        	int numRegistros = 0;
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
		/* Metemos la hora para saber cuando se paso */
		strftime(output,128, "%Y/%m/%d %H:%M:%S",tlocal);
		/* Connect to database */
		if (!mysql_real_connect(conn, server,
        	 user, password, database, 0, NULL, 0)) {
      			fprintf(stderr, "%s\n", mysql_error(conn));
      			digitalWrite(2,LOW);
      			exit(1);
   		}
		strcpy(querySQL,"");
		strcat(querySQL,"SELECT finalizado FROM sat WHERE idSat=");
		strcat(querySQL, numSat);
		if(existeRegistro(conn, querySQL, res, row, "No", 0)){
		/* Si no esta Finalizado, lo finalizamos con todos los datos necesarios */
			strcpy(querySQL,"");
			strcat(querySQL,"UPDATE sat SET finalizado='Si', diagnostico='Reparado', donde='Mesa Roja',finSat='");
			strcat(querySQL,output);
			strcat(querySQL,"', horaFin='");
			strcat(querySQL,output);
			strcat(querySQL,"' WHERE idSat=");
			strcat(querySQL, numSat);
			lanzaQuery(conn, querySQL, res);
			enciendeLed(0);
		}
		else{
			/* Si esta Finalizado preguntamos si pasaron 15 segundos desde que se guardo */
			strcpy(querySQL,"");		
			strcat(querySQL,"SELECT finSat FROM sat WHERE idSat=");
			strcat(querySQL, numSat);
			lanzaQuery(conn, querySQL, res);
			res = mysql_store_result(conn);
			row  = mysql_fetch_row(res);
			/* Hora de tomada paso de String a mktime */
			struct tm tm;
			strptime(row[0], "%Y-%m-%d %H:%M:%S", &tm);
			time_t tomaNota = mktime(&tm);
			/* Hora AHORA */
			time_t now = time(0);
			/* Diferencia horaria */
			printf ("Diferencia de tiempo es %f\n",difftime(now,tomaNota));
			double segundosPasados = difftime(now,tomaNota);
			/* Si han pasado menos de 20 segundos, ponemos como no reparado o reparado constantemente */
			if(segundosPasados < 20.00){
				strcpy(querySQL,""); /*Vacio el string de querySQL*/
				strcat(querySQL,"SELECT diagnostico FROM sat WHERE idSat=");
				strcat(querySQL, numSat);
				if(existeRegistro(conn, querySQL, res, row, "Reparado", 0)){
				/* Si esta como reparado, lo ponemos a No Reparado */			
					strcpy(querySQL,""); /*Vacio el string de querySQL*/
					strcat(querySQL,"UPDATE sat SET diagnostico='No Reparado' WHERE idSat=");
					strcat(querySQL,numSat);
					printf("Ahora esta como No Reparado");
					lanzaQuery(conn,querySQL,res);		
					parpadeaLed(1,5);	
				}
				else{
				/* Si esta como No Reparado, lo marco como Reparado */
					strcpy(querySQL,""); /*Vacio el string de querySQL*/
					strcat(querySQL,"UPDATE sat SET diagnostico='Reparado' WHERE idSat=");
					strcat(querySQL,numSat);
					printf("Ahora esta como Reparado");
					lanzaQuery(conn,querySQL,res);	
					parpadeaLed(0,5);
				}	
			}
			else{
				strcpy (querySQL,"");
				strcat (querySQL,"INSERT INTO segSat(idSat, Accion, descrAccion, fecha, hora, usuario, estado, notas) 					VALUES ('");
				strcat(querySQL,numSat);
				strcat(querySQL,"', 'Llamada', 'Llamada sin respuesta', '");
				strcat(querySQL, output);
				strcat(querySQL, "','");
				strcat(querySQL, output);
				strcat(querySQL,"', 'AUTOMATIZADO', 'Fallido', 'R1Pi')");
				lanzaQuery(conn,querySQL, res);
				printf ("segSat Lanzado: %s",querySQL);
				enciendeDosLeds(2);	
				/* Si han pasado mas de 15 segundos, marcamos llamada */
			}
		}
	/* close connection */
   	/*mysql_free_result(res);*/
  	mysql_close(conn);
	} 
}

/* FUNCIONES VARIAS */

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
   	res = mysql_store_result(conn);
	strcpy (estaFinalizado,""); 
	while(row  = mysql_fetch_row(res)){
		strcat (estaFinalizado,row[0]); 
	}
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
