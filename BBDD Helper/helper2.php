<?php
include('config.php');
$pdo = connect();
// Order by idSat ASC
 $sql = 'SELECT diagnostico,donde,idSat FROM sat';
 $consulta = $pdo->prepare($sql);
        $consulta->execute();
		while ($recorrido = $consulta->fetch()):
		if(strcmp($recorrido['diagnostico'],'Reparado') == 0 && (strcmp($recorrido['donde'],'Taller') == 0)){
			$repara = 'UPDATE sat SET donde="Mesa Roja" WHERE idSat='.$recorrido['idSat'];	
			$query = $pdo->prepare($repara);
		try {	
			$query->execute();
			$query->closeCursor(); 
		}	
		 catch (PDOException $e) {
			echo 'PDOException : '.  $e->getMessage();
		}
		echo 'id sat modificado:'.$recorrido['idSat'].'<br>';
		}elseif(strcmp($recorrido['diagnostico'],'No Reparado') == 0 && (strcmp($recorrido['donde'],'Taller') == 0)){
			$repara = 'UPDATE sat SET donde="Mesa Roja" WHERE idSat='.$recorrido['idSat'];		
			$query = $pdo->prepare($repara);
		try {	
			$query->execute();
			$query->closeCursor(); 
		}
		 catch (PDOException $e) {
			echo 'PDOException : '.  $e->getMessage();
		}
		echo 'id sat modificado:'.$recorrido['idSat'].'<br>';
		}
		endwhile;

	
echo 'hecho';
?>
