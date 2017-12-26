<?php
include('config.php');
$pdo = connect();
// Order by idSat ASC
 $sql = 'SELECT idSegSat,idSat, descrAccion FROM segSat ORDER BY idSegSat ASC';
 $consulta = $pdo->prepare($sql);
        $consulta->execute();
		while ($recorrido = $consulta->fetch()):
		if(strcmp($recorrido['descrAccion'],'Cliente personado en tienda para recogida del aparato') == 0){
			$repara = 'UPDATE sat SET donde="Entregado a cliente" WHERE idSat='.$recorrido['idSat'];		
		}elseif(strcmp($recorrido['descrAccion'],'Llamada sin respuesta') == 0){
			$repara = 'UPDATE sat SET donde="Mesa Roja" WHERE idSat='.$recorrido['idSat'];		
		}elseif(strcmp($recorrido['descrAccion'],'Cliente Avisado por telefono') == 0){
			$repara = 'UPDATE sat SET donde="Recogidas" WHERE idSat='.$recorrido['idSat'];				
		}elseif(strcmp($recorrido['descrAccion'],'Aviso Web') == 0){
			$repara = 'UPDATE sat SET donde="Recogidas" WHERE idSat='.$recorrido['idSat'];						
		}
		$query = $pdo->prepare($repara);
try {	
	$query->execute();
	$query->closeCursor(); 
}
 catch (PDOException $e) {
	echo 'PDOException : '.  $e->getMessage();
}
		echo 'id sat modificado:'.$recorrido['idSat'].'<br>';
		endwhile;

	
echo 'hecho';
?>
