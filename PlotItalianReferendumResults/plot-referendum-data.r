require("ggplot2")
require("ggmap")
require("OpenStreetMap")
library(devtools)
install_github("quantide/mapIT")
require("mapIT")

regionsEN=c("Abruzzo", "Aosta Valley", "Apulia", "Basilicata", "Calabria",
"Campania", "Emilia-Romagna", "Friuli-Venezia Giulia", "Lazio",
"Liguria", "Lombardy", "Marche", "Molise", "Piedmont", "Sardinia",
"Sicily", "Trentino-South Tyrol", "Tuscany", "Umbria", "Veneto",
"Italy", "Europe", "'South America'", "'North America'", "Asia")

regionsIT=c("Abruzzo","Valle d\'Aosta", "Puglia", "Basilicata","Calabria",
"Campania", "Emilia-Romagna", "Friuli-Venezia Giulia", "Lazio",
"Liguria", "Lombardia", "Marche", "Molise", "Piemonte", "Sardegna",
"Sicilia", "Trentino-Alto Adige", "Toscana", "Umbria","Veneto",
"Italia", "Europa", "America meridionale", "American settentrionale e centrale",
"Africa, Asia, Oceania, Antartide")

isRegion=c(rep(TRUE,20),rep(FALSE,5))

electorate=c(1052049, 99735, 3280745, 467000, 1553741,
4566905, 3326910, 952493, 4402145,
1241618, 7480375, 1189180, 256600, 3396378, 1375845,
4031871, 792503, 2854162, 675610, 3725399,
46720943, 2166037, 1291065, 374987, 220252)

percentNo=c(64.4, 56.8, 67.2, 65.9, 67.0,
68.5, 49.6, 61.0, 63.3,
60.1, 55.5, 55.0, 60.8, 56.5, 72.2,
71.6, 46.1, 47.5, 51.2, 61.9,
60.0, 37.6, 28.1, 37.8, 40.3)

referendum=c(rep("No",6),"Yes",rep("No",9),"Yes", "Yes", rep("No", 3), rep("Yes", 4))
my.data <- data.frame(regionsEN, regionsIT, electorate, referendum, isRegion, percentNo)
my.data$regionsEN <- as.character(my.data$regionsEN)
latlon <- geocode(my.data$regionsEN)
my.data<-cbind(my.data,latlon)
my.data.Italy<-subset(my.data,isRegion==TRUE)
my.data.World<-subset(my.data,isRegion==FALSE)
title <- "Referendum 2016 Results"
circle_scale<-0.000004

# generate Italy map
p <- ggmap(get_map(location="Italy",zoom=6), extent="panel")
p <- p + geom_point(aes(x=lon, y=lat),data=my.data.Italy, 
colour=ifelse(my.data.Italy$referendum == "No",'red','green'),
alpha=0.4, size=my.data.Italy$electorate*circle_scale)
p <- p + labs(title=paste(title, "by Region"))
p

# general Italy choropleth map
gp <- list(guide.label="Percent\nNo\nVote", title="2016 Referendum Results by Region", 
low="green", high="red")
mapIT(percentNo, regionsIT, my.data.Italy,  graphPar=gp)

# generate world map
q <- openproj(openmap(c(70,-145), c(-70,145), zoom=1)) 
q <- autoplot(q) + geom_point(aes(x=lon, y=lat),data=my.data.World, 
col=ifelse(my.data.World$referendum == "No",'red','green'),
alpha=0.4, size=log(my.data.World$electorate))
q <- q + labs(x="lon", y="lat", title=paste(title,"Worldwide")) 
q
