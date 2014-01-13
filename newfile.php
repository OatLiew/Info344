
<html>
<body>

<?php 

$last = $_GET["number"];
for ($i = 1; $i<$last+1 ; $i++) {
	
	if ($i % 2 == 0)
	{	
		echo $i;
	}
}


?>

</body>
</html>

