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
int esNulo(MYSQL *conn,char* queryFinalizado, MYSQL_RES *res, MYSQL_ROW row);
int scale [] = { NOTE_E7, NOTE_E7, 0, NOTE_E7, 
  0, NOTE_C7, NOTE_E7, 0,
  NOTE_G7, 0, 0,  0,
  NOTE_G6, 0, 0, 0, } ;

main() {
   MYSQL *conn;
   MYSQL_RES *res;
   MYSQL_ROW row;
   char *server = "YOURADDRESS";
   char *user = "YOURUSER";
   char *password = "YOURPASSWORD"; /* set me first */
   char *database = "YOURDATABASE";
   char *sql = "UPDATE sat SET finalizado='Si',finSat='";
   char *sql1="' WHERE idSat=";
   char *sql2=" AND finalizado='No'";
   char numSat[10];
   char querySQL[100];
   char concatenasql[1000];
   int i;
   wiringPiSetup();
   softToneCreate (PIN) ;
   pinMode (0, OUTPUT);
   pinMode (1, OUTPUT);
   pinMode (2, OUTPUT);
  for(i=0;i<16;i++){
  softToneWrite (PIN, scale [i]) ;
  delay (60) ;
  softToneWrite (PIN, scale [i]) ;
  delay(60);
   }
 
softToneWrite (PIN,0);
while(1 ){
	/* Iniciamos el led azul (en espera) */
	digitalWrite(2,HIGH);
        conn = mysql_init(NULL);
/* Recogemos el n. de SAT por codigo de barras o teclado si es diferente de letras y entre 0 y 100000*/
	do{	
		printf("Introduce el SAT para marcar como finalizado (q para salir(\n");
		scanf("%s",numSat);   
		if(strcmp(numSat,"q") == 0 || strcmp(numSat,"Q")  == 0){
			printf("Has introducido q, nos salimos\n");
			digitalWrite(2,LOW);
			exit(1);
		}
		if(atoi(numSat) <1 || atoi(numSat) > 100000){
		int x;
		for(x=0;x<5;x++){
			digitalWrite(1,HIGH);
			delay(100);
			digitalWrite(1,LOW);
			delay(100);
		}
		}
	}while(atoi(numSat) <1 || atoi(numSat) > 100000);
/* Metemos la hora para saber cuando se paso */
	time_t tiempo = time(0);
	char output[128];
	struct tm *tlocal = localtime(&tiempo);
        int numRegistros = 0;
	strftime(output,128, "%Y/%m/%d %H:%M:%S",tlocal);
/* Connect to database */
   	if (!mysql_real_connect(conn, server,
         user, password, database, 0, NULL, 0)) {
		fprintf(stderr, "%s\n", mysql_error(conn));
  		digitalWrite(2,LOW);
		exit(1);
	}

/* Concatenamos la sql con el numero */
	strcpy(concatenasql,""); /*Vacio el string de concatenasql*/
	strcat(concatenasql,"SELECT salidaReal, horaSR FROM sat WHERE idSat="); 
	strcat(concatenasql,numSat);
	printf("SQL Lanzada; %s\n",concatenasql);
/* Pregunto si salidaReal y horaSR son Nulos (valor \0) */
	if(esNulo(conn,concatenasql,res,row)){
		printf("NO TIENE FECHAS DE SALIDA\n");		
		/* si los valores son nulos no ha salido, pregunto si esta finalizado */
		strcpy(concatenasql,""); /*Vacio el string de concatenasql*/
		strcat(concatenasql,"SELECT finalizado FROM sat WHERE idSat="); 
		strcat(concatenasql,numSat);
		printf("SQL Lanzada; %s\n",concatenasql);	
		if(existeRegistro(conn, concatenasql, res, row, "No",0)){
		printf("NO TIENE FECHA DE SALIDA NI ESTA FINALIZADO\n");
		/* Si esta sin finalizar lo meto como no reparado */
			strcpy(concatenasql,""); /*Vacio el string de concatenasql*/
			strcat(concatenasql,"UPDATE sat SET finalizado='Si', diagnostico='No Reparado',salidaReal='");
			strcat(concatenasql,output);
			strcat(concatenasql,"', horaSR='");
			strcat(concatenasql,output);
			strcat(concatenasql,"', horaFin='");
			strcat(concatenasql,output);
			strcat(concatenasql,"', finSat='");
			strcat(concatenasql,output);
			strcat(concatenasql,"', donde='Entregado a cliente', avisado='Si' WHERE idSat=");
			strcat(concatenasql,numSat);
			lanzaQuery(conn,concatenasql,res);
			strcpy (concatenasql,"");
			strcat (concatenasql,"INSERT INTO segSat(idSat, Accion, descrAccion, fecha, hora, usuario, estado, notas) 				VALUES ('");
			strcat(concatenasql,numSat);
			strcat(concatenasql,"', 'Cita en taller', 'Cliente personado en tienda para recogida del aparato', '");
			strcat(concatenasql, output);
			strcat(concatenasql, "','");
			strcat(concatenasql, output);
			strcat(concatenasql,"', 'AUTOMATIZADO', 'Completado', 'R3Pi')");
			printf ("segSat Lanzado: %s",concatenasql);
			lanzaQuery(conn,concatenasql, res);		
			enciendeLed(1);		
		}
		else{
		printf("NO TIENE FECHA DE SALIDA Y SI ESTA FINALIZADO\n");
		/*Si esta finalizado no toco el estado de reparacion */
			strcpy(concatenasql,""); /*Vacio el string de concatenasql*/
			strcat(concatenasql,"UPDATE sat SET salidaReal='");
			strcat(concatenasql,output);
			strcat(concatenasql,"', horaSR='");
			strcat(concatenasql,output);
			strcat(concatenasql,"', donde='Entregado a cliente', avisado='Si' WHERE idSat=");
			strcat(concatenasql,numSat);
			lanzaQuery(conn,concatenasql,res);		
			strcpy (concatenasql,"");
			strcat (concatenasql,"INSERT INTO segSat(idSat, Accion, descrAccion, fecha, hora, usuario, estado, notas) 				VALUES ('");
			strcat(concatenasql,numSat);
			strcat(concatenasql,"', 'Cita en taller', 'Cliente personado en tienda para recogida del aparato', '");
			strcat(concatenasql, output);
			strcat(concatenasql, "','");
			strcat(concatenasql, output);
			strcat(concatenasql,"', 'AUTOMATIZADO', 'Completado', 'R3Pi')");
			lanzaQuery(conn,concatenasql, res);
			printf ("segSat Lanzado: %s",concatenasql);
			enciendeLed(0);	
		}
	}
	else{
		/* Si no esta nulo hora recogida cambiamos estado de reparacion a reparado o no reparado */
			strcpy(concatenasql,""); /*Vacio el string de concatenasql*/
			strcat(concatenasql,"SELECT diagnostico FROM sat WHERE idSat=");
			strcat(concatenasql,numSat);
			if(existeRegistro(conn, concatenasql, res, row, "Reparado",0)){
			/* Si esta como Reparado, lo marco como NO REPARADO */
				strcpy(concatenasql,""); /*Vacio el string de concatenasql*/
				strcat(concatenasql,"UPDATE sat SET diagnostico='No Reparado' WHERE idSat=");
				strcat(concatenasql,numSat);
				printf("Ahora esta como No Reparado");
				lanzaQuery(conn,concatenasql,res);		
				parpadeaLed(1,5);	
			}
			else{
			/* Si esta como No Reparado, lo marco como Reparado */
				strcpy(concatenasql,""); /*Vacio el string de concatenasql*/
				strcat(concatenasql,"UPDATE sat SET diagnostico='Reparado' WHERE idSat=");
				strcat(concatenasql,numSat);
				printf("Ahora esta como Reparado");
				lanzaQuery(conn,concatenasql,res);	
				parpadeaLed(0,5);
			}	
		}
	
	printf("%ld registros modificados\n",(long) mysql_affected_rows(conn));

  /* close connection */
	/*mysql_free_result(res);*/
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
int esNulo(MYSQL *conn,char* queryFinalizado, MYSQL_RES *res, MYSQL_ROW row){
	if (mysql_query(conn, queryFinalizado)) {
      		fprintf(stderr, "%s\n", mysql_error(conn));
      		digitalWrite(2,LOW);
      		exit(1);
   	}
	/* Guardamos el resultasdo en res */
   	res = mysql_store_result(conn);
	row  = mysql_fetch_row(res);
		/* Si esta finalizado lo marcaremos como avisado */
	/* strcmp devuelve un 0 si las 2 cadenas son identicas y un <0 o >0 si son diferentes */
	if(row[0] == NULL || row[1] == NULL){
		/*printf("Voy a devolver un 1");*/
		return 1;
	}
	else{
		/*printf("Voy a devolver un 0");*/
		return 0;
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
	/* Si esta finalizado lo marcaremos como avisado */
	/* strcmp devuelve un 0 si las 2 cadenas son identicas y un <0 o >0 si son diferentes */
	if(strcmp(estaFinalizado,valor) == resultadoNecesario){
		/*printf("Voy a devolver un 1");*/
		return 1;
	}
	else{
		/*printf("Voy a devolver un 0");*/
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
	delay(650);
	digitalWrite(color,LOW);
}
void parpadeaLed(int color, int numVeces){
	/* Parpadea numVeces veces rapidas el led rojo */
	int x;
	for(x=0;x<numVeces;x++){
		digitalWrite(color,HIGH);
		delay(100);
		digitalWrite(color,LOW);
		delay(100);
	}
} 
