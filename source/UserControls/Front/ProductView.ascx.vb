'========================================================================
'Kartris - www.kartris.com
'Copyright 2016 CACTUSOFT

'GNU GENERAL PUBLIC LICENSE v2
'This program is free software distributed under the GPL without any
'warranty.
'www.gnu.org/licenses/gpl-2.0.html

'KARTRIS COMMERCIAL LICENSE
'If a valid license.config issued by Cactusoft is present, the KCL
'overrides the GPL v2.
'www.kartris.com/t-Kartris-Commercial-License.aspx
'========================================================================
Imports CkartrisDataManipulation
Imports CkartrisImages

''' <summary>
''' Used in the Products.aspx, it loads all the sections (as UCs) that are related to the Current Product.
'''   Attributes, Versions, Promotions, Reviews, and CarryOnShopping.
''' </summary>
''' <remarks>By Mohammad</remarks>
Partial Class ProductView
    Inherits System.Web.UI.UserControl

    Private _ProductID As Integer
    Private _LanguageID As Short
    Private _ProductName As String
    Private _DisplayType As Char
    Private _ReviewsEnabled As Char
    Private _IsProductExit As Boolean

    Public ReadOnly Property ProductID() As Integer
        Get
            Return _ProductID
        End Get
    End Property

    Public ReadOnly Property LanguageID() As Short
        Get
            Return _LanguageID
        End Get
    End Property

    Public ReadOnly Property DisplayType() As Char
        Get
            Return _DisplayType
        End Get
    End Property

    Public ReadOnly Property IsProductExist() As Boolean
        Get
            Return _IsProductExit
        End Get
    End Property

    Public ReadOnly Property ProductName() As String
        Get
            Return _ProductName
        End Get
    End Property

    ''' <summary>
    ''' Loads/Shows the Product Info.
    ''' </summary>
    ''' <param name="pProductID"></param>
    ''' <param name="pLanguageID"></param>
    ''' <remarks>By Mohammad</remarks>
    Public Sub LoadProduct(ByVal pProductID As Integer, ByVal pLanguageID As Short)
        _ProductID = pProductID
        _LanguageID = pLanguageID

        'Gets the details of the current product
        Dim tblProducts As New DataTable
        tblProducts = ProductsBLL.GetProductDetailsByID(_ProductID, _LanguageID)

        'If there is no products returned, then go to the categories page.
        If tblProducts.Rows.Count = 0 Then _IsProductExit = False : Exit Sub

        'Checking the customer group of the category
        Dim numCGroupID As Short = 0, numParentGroup As Short = 0, numCurrentGroup As Short = 0
        If HttpContext.Current.User.Identity.IsAuthenticated Then
            numCGroupID = CShort(DirectCast(Page, PageBaseClass).CurrentLoggedUser.CustomerGroupID)
        End If

        numCurrentGroup = FixNullFromDB(tblProducts.Rows(0)("P_CustomerGroupID"))
        Try
            Dim node As SiteMapNode = SiteMap.CurrentNode
            numParentGroup = node.ParentNode("CG_ID")
            If numParentGroup <> 0 AndAlso numParentGroup <> numCGroupID Then
                _IsProductExit = False : Exit Sub
            ElseIf numCurrentGroup <> 0 AndAlso numCurrentGroup <> numCGroupID Then
                _IsProductExit = False : Exit Sub
            End If
        Catch ex As Exception
            If numCurrentGroup <> 0 AndAlso numCurrentGroup <> numCGroupID Then
                _IsProductExit = False : Exit Sub
            End If
        End Try

        _IsProductExit = True

        _ProductName = FixNullFromDB(tblProducts.Rows(0)("P_Name"))
        Dim strStrapline As String = FixNullFromDB(tblProducts.Rows(0)("P_Strapline"))
        _DisplayType = FixNullFromDB(tblProducts.Rows(0)("P_VersionDisplayType"))

        'Checking if the reviews are enabled for the Product.
        _ReviewsEnabled = IIf(tblProducts.Rows(0)("P_Reviews") Is DBNull.Value, "n", tblProducts.Rows(0)("P_Reviews"))

        'Product's Page Title
        Dim strPageTitle As String = FixNullFromDB(tblProducts.Rows(0)("P_PageTitle"))
        Page.Title = IIf(strPageTitle = "", _
                         _ProductName & " | " & Server.HtmlEncode(GetGlobalResourceObject("Kartris", "Config_Webshopname")), _
                         strPageTitle & " | " & Server.HtmlEncode(GetGlobalResourceObject("Kartris", "Config_Webshopname")))

        tblProducts.Rows(0)("MinPrice") = CurrenciesBLL.ConvertCurrency(Session("CUR_ID"), FixNullFromDB(tblProducts.Rows(0)("MinPrice")))

        'Set H1 tag
        litProductName.Text = Server.HtmlEncode(_ProductName)
        litProductStrapLine.Text = Server.HtmlEncode(strStrapline)

        'Bind the DataTable to the FormView that is used to view the Product's Info.
        fvwProduct.DataSource = tblProducts
        fvwProduct.DataBind()

        ' Build the image viewers
        BuildImageViewer()

        UC_MediaGallery.ParentID = _ProductID

        '-------------------------------------
        'MEDIA POPUP
        '-------------------------------------
        'We set width and height later with
        'javascript, as popup size will vary
        'depending on the media type

        'UC_PopUpMedia.SetTitle = _ProductName 'blank this out to match Foundation popup for large images which has no title
        UC_PopUpMedia.SetMediaPath = _ProductID

        UC_PopUpMedia.PreLoadPopup()

    End Sub

    ''' <summary>
    ''' Build the image viewers on the page.
    ''' </summary>
    ''' <remarks>Refractered out by Craig Moore so that it can be called from several places</remarks>
    Private Sub BuildImageViewer()
        Dim strProductFolder As String = String.Empty       ' Folder where images are located.
        'strProductFolder = CStr(ProductID)
        'Dim sos As List(Of SelectedOption) = SelectedOptions
        'If Not IsNothing(sos) Then
        '    ' If a product option (swatch) has been selected in an option control, we need to append its details to the 
        '    ' image folder that the image control is going to go looking for the new images.
        '    ' E.g. Product 5 with swatch option 11 would be looking in  /Images/Products/5/11.
        '    For Each so As SelectedOption In sos

        '    Next
        '    strProductFolder = strProductFolder & "/" & CStr(ProductOptionId)
        'End If

        strProductFolder = BuildImagePath(ProductID, SelectedOptions)

        'Create the image view 
        UC_ImageView.ClearImages()
        UC_ImageView.CreateImageViewer(IMAGE_TYPE.enum_ProductImage, _
            strProductFolder, _
            KartSettingsManager.GetKartConfig("frontend.display.images.normal.height"), _
            KartSettingsManager.GetKartConfig("frontend.display.images.normal.width"), _
            "", _
            "", , _ProductName)
        'End If
        'We have two types of largeview links, depending on the config settings.
        'One type is an AJAX popup, with the large image resized to fit.
        'The other is a new page, with the large image full size.
        If KartSettingsManager.GetKartConfig("frontend.display.images.large.linktype") = "n" Then

        Else
            'If tblProducts.Rows(0)("P_Desc").ToString.Contains("<overridelargeimagelinktype>") Then
            If CBool(ObjectConfigBLL.GetValue("K:product.showlargeimageinline", ProductID)) Then
                'Override triggered - for MTMC large images
                UC_ImageView.Visible = False

                'Need to override the Foundation column widths
                'To make sure both full 12 width, so image
                'stacks over text
                litImageColumnClasses.Text = "imagecolumn small-12 columns"
                litTextColumnClasses.Text = "textcolumn small-12 columns"

                'Set full size image visible
                UC_ImageView2.CreateImageViewer(IMAGE_TYPE.enum_ProductImage,
                    strProductFolder,
                    0,
                    0,
                    "",
                    "rrr")
                UC_ImageView2.Visible = True
            End If

        End If
    End Sub

    ''' <summary>
    ''' Get the path to the subfolder that contains the product image with the selected options.
    ''' </summary>
    ''' <param name="ProductId">the unique identified for the parent product</param>
    ''' <param name="Options">A list of options that will help us find the selected product option images</param>
    ''' <returns></returns>
    ''' <remarks>Product options are in a nested folder format, so folder itteration is required here. This is only the subfolder path, not the actual images path</remarks>
    Private Function BuildImagePath(ByVal ProductId As Integer,
                                    Optional ByVal Options As List(Of SelectedOption) = Nothing) As String

        Dim strPath = CkartrisImages.strProductImagesPath
        Dim strSubPath As String = CStr(ProductId)
        Dim dirFolder As DirectoryInfo
        Dim MatchFound As Boolean = False
        dirFolder = New DirectoryInfo(Server.MapPath(strPath & "/" & strSubPath))
        If dirFolder.Exists Then
            If Not IsNothing(Options) Then
                ' We have some options which may have images to manage.
                Do
                    ' Loop through all of the options and try to find a subfolder in the current path
                    ' which has a reference that matches the current option ID
                    MatchFound = False
                    For Each so As SelectedOption In Options
                        dirFolder = New DirectoryInfo(Server.MapPath(strPath & "/" & strSubPath) & "/" & so.OptionId.ToString)
                        If dirFolder.Exists Then
                            ' We have found a matching folder.
                            MatchFound = True
                            strSubPath = strSubPath & "/" & so.OptionId.ToString
                            Exit For
                        End If
                    Next
                    ' If a matching sub folder was found on this itteration, loop again and see
                    ' if we can find another matching subfolder.
                Loop While MatchFound = True
            End If
        End If

        ' Return the path that we have built
        Return strSubPath
        
    End Function

    ''' <summary>
    ''' Triggered with the option swatch control has selected a different swatch option
    ''' </summary>
    ''' <param name="OptionParentID">Option group ID</param>
    ''' <param name="OptionId">Option ID</param>
    ''' <param name="OptionText">Option display text</param>
    ''' <remarks>Changes the images that are currently displayed to those associated with the selection swatch option.</remarks>
    Private Sub OptionSwatchChanged(ByVal OptionParentID As Integer, OptionId As Integer, OptionText As String) Handles UC_ProductVersions.OptionsSwatchChanged

        If (RecordOptionSelection(OptionParentID, OptionId, OptionText)) Then
            ' Change was detected refresh the image display
            BuildImageViewer()
            updImages.Update()
            ' trigger the Foundation script to run again if we are performing a partial page postback with updated images. 
            ' This is because the foundation script only runs on page load, not on partial postback, and we need it to run to format the carousel for 
            ' the images.
            ' The script is here, and not in the image viewer control because the image viewer control gets redrawn in the code behind even if it is not
            ' being posted to the client, and it is a waste of resources to trigger the script every postback. It is only from within this current method
            ' that we know why the redraw is occuring and we know that we are updating the image update panel.
            ScriptManager.RegisterStartupScript(Page, Me.GetType(), "FoundationClearing", "$(document).foundation();", True)
        End If
    End Sub

    ''' <summary>
    ''' Record which option was selected for future use.
    ''' </summary>
    ''' <param name="OptionParentID">Option group ID</param>
    ''' <param name="OptionId">Option ID</param>
    ''' <param name="OptionText">Option display text</param>
    ''' <returns>True if a change is detected.</returns>
    ''' <remarks></remarks>
    Private Function RecordOptionSelection(ByVal OptionParentID As Integer, OptionId As Integer, OptionText As String) As Boolean
        ' Records the option that was selected.
        ' If the option is new, we add it.
        ' If the option is existing, we replace it.
        RecordOptionSelection = False   ' Initial value
        Dim sos As List(Of SelectedOption) = SelectedOptions
        Dim so As SelectedOption
        Dim soNew As New SelectedOption
        If IsNothing(sos) Then
            sos = New List(Of SelectedOption)
        End If

        ' Build the new swatch option
        With soNew
            .ParentID = OptionParentID
            .OptionId = OptionId
            .OptionText = OptionText
        End With

        so = sos.Find(Function(p) p.ParentID = OptionParentID)
        If Not IsNothing(so) Then
            ' Does already exist, replace it.
            If so.OptionId <> soNew.OptionId Then
                ' Change detected
                RecordOptionSelection = True
            End If
            ' Remove, ready for insert
            sos.RemoveAt(sos.IndexOf(so))
        Else
            ' Does not exist so a change must have taken place
            RecordOptionSelection = True
        End If

        ' Add the new one
        sos.Add(soNew)

        ' Save the changes
        SelectedOptions = sos

    End Function

    ''' <summary>
    ''' The image swatch options that have been selected
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Property SelectedOptions As List(Of SelectedOption)
        Get
            If IsNothing(ViewState("swatchoptions")) Then
                Return Nothing
            ElseIf ViewState("swatchoptions").ToString.Length = 0 Then
                Return Nothing
            Else
                ' We have a value in viewstate, we just need to extract it now.
                Dim ret As New List(Of SelectedOption)
                Dim so As SelectedOption
                Dim OptionItems()() As String = ViewState("swatchoptions")
                Dim i As Integer
                For i = 0 To OptionItems.GetUpperBound(0)
                    ' Itterate the list of swatch options in the jagged array
                    so = New SelectedOption
                    so.ParentID = CInt(OptionItems(i)(0))
                    so.OptionId = CInt(OptionItems(i)(1))
                    so.OptionText = OptionItems(i)(2)
                    ' Add item extracted from jagged array to list for return
                    ret.Add(so)
                Next
                ' Return list
                Return ret
            End If
        End Get
        Set(value As List(Of SelectedOption))
            If value.Count > 0 Then
                ' We have swatch options, create a jagged array and record each swatch option as a string array, 
                ' then add these to a master string array which will be stored in the viewstate.
                Dim OptionItems()() As String
                Dim i As Integer
                OptionItems = New String(value.Count - 1)() {}
                For i = 0 To value.Count - 1
                    OptionItems(i) = New String() {value(i).ParentID.ToString, value(i).OptionId.ToString, value(i).ParentID}
                Next i
                ' Store jagged array in viewstate
                ViewState("swatchoptions") = OptionItems
            Else
                ViewState("swatchoptions") = String.Empty
            End If
        End Set
    End Property

    Private Class SelectedOption
        Public Property ParentID As Integer
        Public Property OptionId As Integer
        Public Property OptionText As String
    End Class


    Protected Sub fvwProduct_DataBound(ByVal sender As Object, ByVal e As System.EventArgs) Handles fvwProduct.DataBound

        'We will set number of visible tabs and
        'keep track. This is only required for
        'mobile display, so we can have numbered
        'tabs instead of named ones to keep the
        'page width down.
        Dim numVisibleTabs As Integer = 1

        'Handle compare link
        SetCompareLink()

        'Handle price display
        Dim litPriceTemp As Literal = CType(fvwProduct.FindControl("litPrice"), Literal)

        '=================================
        'Handle main tab
        '=================================
        Dim litContentTextHome As Literal = CType(tabMain.FindControl("litContentTextHome"), Literal)

        '=================================
        'Handle attributes tab
        '=================================
        UC_ProductAttributes.LoadProductAttributes(_ProductID, LanguageID)

        'If no attributes, then hide tab
        If UC_ProductAttributes.Visible = False Then
            Dim tabAttributes As AjaxControlToolkit.TabPanel = CType(tbcProduct.FindControl("tabAttributes"), AjaxControlToolkit.TabPanel) 'Finds tab panel in container
            tabAttributes.Enabled = False
            tabAttributes.Visible = False
        Else
            numVisibleTabs += 1
            Dim litContentTextAttributes As Literal = CType(tabAttributes.FindControl("litContentTextAttributes"), Literal)
        End If

        '=================================
        'Handle quantity discounts tab
        '=================================
        ' Check if Call for Price Product, will not process quantity discount
        If ObjectConfigBLL.GetValue("K:product.callforprice", _ProductID) <> 1 Then
            UC_QuantityDiscounts.LoadProductQuantityDiscounts(_ProductID, _LanguageID)
        Else
            UC_QuantityDiscounts.Visible = False
        End If

        'If no quantity discounts, then hide tab
        If UC_QuantityDiscounts.Visible = False Then
            Dim tabQuantityDiscounts As AjaxControlToolkit.TabPanel = CType(tbcProduct.FindControl("tabQuantityDiscounts"), AjaxControlToolkit.TabPanel) 'Finds tab panel in container
            tabQuantityDiscounts.Enabled = False
            tabQuantityDiscounts.Visible = False
        Else
            numVisibleTabs += 1
            Dim litContentTextViewQuantityDiscount As Literal = CType(tabQuantityDiscounts.FindControl("litContentTextViewQuantityDiscount"), Literal)
        End If

        '=================================
        'Handle reviews tab
        '=================================
        Dim tabReviews As AjaxControlToolkit.TabPanel = CType(tbcProduct.FindControl("tabReviews"), AjaxControlToolkit.TabPanel) 'Finds tab panel in container
        If KartSettingsManager.GetKartConfig("frontend.reviews.enabled") = "y" AndAlso _ReviewsEnabled <> "n" Then
            numVisibleTabs += 1
            UC_Reviews.LoadReviews(_ProductID, _LanguageID, _ProductName)
            Dim litContentTextCustomerReviews As Literal = CType(tabReviews.FindControl("litContentTextCustomerReviews"), Literal)
        Else
            tabReviews.Visible = False
            tabReviews.Enabled = False
        End If

        'Handle versions
        UC_ProductVersions.LoadProductVersions(_ProductID, _LanguageID, _DisplayType)

        'Handle promotions
        UC_Promotions.LoadProductPromotions(_ProductID, _LanguageID)
        If KartSettingsManager.GetKartConfig("frontend.promotions.enabled") = "y" Then
            UC_Promotions.Visible = True
        Else
            UC_Promotions.Visible = False
        End If

    End Sub

    Sub SetCompareLink()
        Dim tabMain As AjaxControlToolkit.TabPanel = CType(tbcProduct.FindControl("tabMain"), AjaxControlToolkit.TabPanel)
        Dim fvwProduct As FormView = CType(tabMain.FindControl("fvwProduct"), FormView)

        'If comparison is enabled, proceed, else hide link
        If LCase(KartSettingsManager.GetKartConfig("frontend.products.comparison.enabled")) <> "n" Then

            'Is this coming from comparison page?
            If Request.QueryString.ToString.ToLower.Contains("strpagehistory=compare") Then
                CType(fvwProduct.FindControl("phdCompareLink"), PlaceHolder).Visible = False
            Else
                CType(fvwProduct.FindControl("phdCompareLink"), PlaceHolder).Visible = True

                'Setting the Compare URL ...
                Dim strCompareLink As String = Request.Url.ToString.ToLower
                If Request.Url.ToString.ToLower.Contains("category.aspx") Then
                    strCompareLink = strCompareLink.Replace("category.aspx", "Compare.aspx")
                ElseIf Request.Url.ToString.ToLower.Contains("product.aspx") Then
                    strCompareLink = strCompareLink.Replace("product.aspx", "Compare.aspx")
                Else
                    strCompareLink = "~/Compare.aspx"
                End If
                If strCompareLink.Contains("?") Then
                    strCompareLink += "&action=add&id=" & _ProductID
                Else
                    strCompareLink += "?action=add&id=" & _ProductID
                End If

                CType(fvwProduct.FindControl("lnkCompare"), HyperLink).NavigateUrl = strCompareLink
            End If
        Else
            CType(fvwProduct.FindControl("phdCompareLink"), PlaceHolder).Visible = False
        End If

    End Sub

    Function ShowLineBreaks(ByVal strInput As Object) As String
        Dim strOutput As String = CStr(CkartrisDataManipulation.FixNullFromDB(strInput))
        If strOutput IsNot Nothing Then
            If InStr(strInput, "<") > 0 And InStr(strInput, ">") > 0 Then
                'Input probably contains HTML, so we want to ignore line
                'breaks for display purposes

                'Do nothing
            Else
                strOutput = Replace(strOutput, vbCrLf, "<br />" & vbCrLf)
                strOutput = Replace(strOutput, vbLf, "<br />" & vbCrLf)
            End If
        End If

        Return strOutput
    End Function
End Class
