<?xml version="1.0"?>

<!--
 *
 * Author: 			Andy Ward
 * Organisation: 	xbim Ltd
 * Date: 			2024.06.05
 * e-Mail: 			info@vsk-software.com
 * 
 * Applies transformation changes from IDS 0.9.7 to 1.0.0 RTM
 *
-->

<xsl:stylesheet
        xmlns:ids="http://standards.buildingsmart.org/IDS"
        xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
        xmlns:xs="http://www.w3.org/2001/XMLSchema"
        xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
        version="1.0">

  <xsl:output method="xml" indent="yes"/>

  <xsl:template match="node()|@*">
    <xsl:copy>
      <xsl:apply-templates select="node()|@*"/>
    </xsl:copy>
  </xsl:template>

  <!-- Update Schemaversion location-->
  <xsl:template match="@xsi:schemaLocation">
    <xsl:attribute name="xsi:schemaLocation">http://standards.buildingsmart.org/IDS http://standards.buildingsmart.org/IDS/1.0.0/ids.xsd</xsl:attribute>
  </xsl:template>

</xsl:stylesheet>
