# http://blog.travelmarx.com/2018/02/mapping-and-comparing-the-murder-rates-of-italy-and-the-USA-using-r.html
require("dplyr")
require("rworldmap")
data(countryExData)

# www.nationmaster.com/country-info/stats/Crime/Violent-crime/Murder-rate-per-million-people
data.murder <- read.csv("Murder rate per million people.csv")
colnames(data.murder)[2] <- "Rate"

# get values for Italy and USA
rate.Italy <- data.murder[data.murder$Country=="Italy","Rate"]
rate.USA <- data.murder[data.murder$Country=="United States", "Rate"]

data.murder.Italy <- data.murder
data.murder.USA <- data.murder
data.murder.Italy$RateF <- mutate(data.murder, RateF=ifelse(Rate/rate.Italy >= 1, ifelse(Rate/rate.Italy == 1.0, 1.0, 2.0),0.0))$RateF
data.murder.USA$RateF <- mutate(data.murder, RateF=ifelse(Rate/rate.USA >= 1, ifelse(Rate/rate.USA == 1.0, 1.0, 2.0),0.0))$RateF

# Plotting variables
title <- "Country homicide crimes relative to"
pal <- c("lightgreen", "lightskyblue", "mistyrose2")

# World map referenced to Italy
data.joined.Italy <- joinCountryData2Map(data.murder.Italy, joinCode="NAME", nameJoinColumn="Country")
par(mai=c(0,0,0,0),xaxs="i",yaxs="i")
mapCountryData(data.joined.Italy, nameColumnToPlot="RateF")
mapParams <- mapCountryData(data.joined.Italy, nameColumnToPlot="RateF", addLegend=FALSE, catMethod="fixedWidth", numCats=3, colourPalette=pal, mapTitle="") 
do.call( addMapLegend, c(mapParams, legendWidth=0.5, legendMar = 2, legendLabels = "none" ))
text(x=0,y=120,labels=paste(title,"Italy"), cex=1.5)
text(c(-100,0,100),-140, labels=c("Rates less than Italy","Italy","Rates greater than Italy"))

# Europe map referenced to Italy
data.joined.Italy2 <- joinCountryData2Map(data.murder.Italy, joinCode="NAME", nameJoinColumn="Country")
par(mai=c(0,0,0,0),xaxs="i",yaxs="i")
mapCountryData(data.joined.Italy2, nameColumnToPlot="RateF")
mapParams <- mapCountryData(data.joined.Italy2, nameColumnToPlot="RateF", addLegend=FALSE, catMethod="fixedWidth", numCats=3, colourPalette=pal, mapTitle="", mapRegion="europe") 
do.call( addMapLegend, c(mapParams, legendWidth=0.5, legendMar = 2, legendLabels = "none" ))
text(x=17,y=75,labels=paste(title,"Italy"), cex=1.5)
text(c(2,17,32),30, labels=c("Rates less than Italy","Italy","Rates greater than Italy"))

# World map referenced to USA
data.joined.USA <- joinCountryData2Map(data.murder.USA, joinCode="NAME", nameJoinColumn="Country")
par(mai=c(0,0,0,0),xaxs="i",yaxs="i")
mapCountryData(data.joined.USA, nameColumnToPlot="RateF" )
mapParams <- mapCountryData(data.joined.USA, nameColumnToPlot="RateF", addLegend=FALSE, catMethod="fixedWidth", numCats=3, colourPalette=pal, mapTitle="" ) 
do.call( addMapLegend, c(mapParams, legendWidth=0.5, legendMar = 2, legendLabels = "none" ))
text(x=0,y=120,labels=paste(title,"USA"), cex=1.5)
text(c(-100,0,100),-140, labels=c("Rates less than USA","USA","Rates greater than USA"))
