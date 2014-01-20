
var app=angular.module('myApp', []);

//Onload page which load all the player datas from RDS then converted to JSON
app.factory('myService', function($http, $c) {
  var deffered = $c.defer();
  var data = [];  
  var myService = {};

  myService.async = function() {
    $http.get('search.php')
    .success(function (d) {
      data = d;
      console.log(d);
      deffered.resolve();
    });
    return deffered.promise;
  };

  myService.data = function() { return data; };
  return myService;
});

//input data from myService to result, result = JSON data from query
app.controller('SearchCtrl', function( myService,$scope) {
  myService.async().then(function() {
    $scope.result = myService.data();

  });
});



