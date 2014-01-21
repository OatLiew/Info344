var app = angular.module('myApp', []);

//onpage load function which will call http request
app.factory('myService', function($http) {
  var promise;
  var myService = {
    async: function() {
      if ( !promise ) {
        promise = $http.get('search.php').then(function (response) {
          console.log(response);
          return response.data;
        });
      }
      // Return the promise to the controller
      return promise;
    }
  };
  return myService;
});

//the Json datas are inserted in result
app.controller('MainCtrl', function( myService,$scope) {
   myService.async().then(function(d) {
    $scope.result = d;
  });
});