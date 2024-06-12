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
  
  
  <!-- Update ifcVersion IFC4X3 to latest agreed moniker. Uses a template to replace text since replace() is only in XSLT2 -->
  <xsl:template name="string-replace-all">
    <xsl:param name="text" />
    <xsl:param name="replace" />
    <xsl:param name="by" />
    <xsl:choose>
      <xsl:when test="contains($text, $replace)">
        <xsl:value-of select="substring-before($text,$replace)" />
        <xsl:value-of select="$by" />
        <xsl:call-template name="string-replace-all">
          <xsl:with-param name="text" select="substring-after($text,$replace)" />
          <xsl:with-param name="replace" select="$replace" />
          <xsl:with-param name="by" select="$by" />
        </xsl:call-template>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="$text" />
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
  
  <xsl:template match="ids:specification/@ifcVersion" >
    <xsl:attribute name="ifcVersion">
      <xsl:choose>
        <!-- If IFC4X3 is present and not already updated-->
        <xsl:when test="contains(., 'IFC4X3') and not(contains(., 'IFC4X3_ADD2'))">
          <xsl:variable name="replaced">
            <xsl:call-template name="string-replace-all">
              <xsl:with-param name="text" select="." />
              <xsl:with-param name="replace" select="'IFC4X3'" />
              <xsl:with-param name="by" select="'IFC4X3_ADD2'" />
            </xsl:call-template>
          </xsl:variable>
          <xsl:value-of select="$replaced"/>
        </xsl:when>
        <xsl:otherwise>
          <!-- Keep existing value -->
          <xsl:value-of select="."/>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:attribute>
  </xsl:template>


  <!-- Update Schemaversion location-->
  <xsl:template match="@xsi:schemaLocation">
    <xsl:attribute name="xsi:schemaLocation">http://standards.buildingsmart.org/IDS http://standards.buildingsmart.org/IDS/1.0/ids.xsd</xsl:attribute>
  </xsl:template>

</xsl:stylesheet>
