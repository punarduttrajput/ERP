param serverName string
param logSlowQueries bool = true
param longQueryTime int = 1  // seconds — log queries taking > 1 second (conservative; alert at 500ms separately)
param location string = resourceGroup().location

resource mysqlServer 'Microsoft.DBforMySQL/flexibleServers@2023-06-30' existing = {
  name: serverName
}

resource slowQueryConfig 'Microsoft.DBforMySQL/flexibleServers/configurations@2023-06-30' = {
  parent: mysqlServer
  name: 'slow_query_log'
  properties: {
    value: logSlowQueries ? 'ON' : 'OFF'
    source: 'user-override'
  }
}

resource longQueryTimeConfig 'Microsoft.DBforMySQL/flexibleServers/configurations@2023-06-30' = {
  parent: mysqlServer
  name: 'long_query_time'
  properties: {
    value: string(longQueryTime)
    source: 'user-override'
  }
}

resource logQueriesNotUsingIndexes 'Microsoft.DBforMySQL/flexibleServers/configurations@2023-06-30' = {
  parent: mysqlServer
  name: 'log_queries_not_using_indexes'
  properties: {
    value: 'ON'
    source: 'user-override'
  }
}
